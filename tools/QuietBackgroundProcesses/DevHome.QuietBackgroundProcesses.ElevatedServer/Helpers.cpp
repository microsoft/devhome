// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <fstream>
#include <numeric>
#include <span>
#include <string>
#include <vector>

#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/win32_helpers.h>

#include "DevHomeTelemetryProvider.h"
#include "PerformanceRecorderEngine.h"
#include "Helpers.h"

void WritePerformanceDataToDisk(_In_ PCWSTR path, const std::span<ProcessPerformanceSummary>& data)
{
    std::ofstream file(path, std::ios::binary);
    if (!file.is_open())
    {
        // Handle error
        return;
    }

    for (const auto& item : data)
    {
        file.write(reinterpret_cast<const char*>(&item), sizeof(ProcessPerformanceSummary));
    }

    file.close();
}

std::vector<ProcessPerformanceSummary> ReadPerformanceDataFromDisk(_In_ PCWSTR path)
{
    std::vector<ProcessPerformanceSummary> data;

    std::ifstream file(path, std::ios::binary);
    THROW_WIN32_IF(ERROR_SHARING_VIOLATION, !file.is_open());

    ProcessPerformanceSummary item;
    while (file.read(reinterpret_cast<char*>(&item), sizeof(ProcessPerformanceSummary)))
    {
        data.push_back(item);
    }

    file.close();
    return data;
}

struct ComputerInformation
{
    DWORD processorCount;
    std::wstring processor;
    std::wstring motherboard;
    DWORDLONG ram;
};

// Get computer information
ComputerInformation GetComputerInformation()
{
    ComputerInformation computerInfo;

    // Get processor information
    SYSTEM_INFO systemInfo = { 0 };
    GetSystemInfo(&systemInfo);
    computerInfo.processorCount = systemInfo.dwNumberOfProcessors;

    // Get processor make and model using win32 apis
    wil::unique_hkey hKey;
    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        wchar_t processorName[256] = { 0 };
        DWORD processorNameSize = sizeof(processorName);
        if (RegQueryValueEx(hKey.get(), L"ProcessorNameString", nullptr, nullptr, reinterpret_cast<BYTE*>(processorName), &processorNameSize) == ERROR_SUCCESS)
        {
            computerInfo.processor = processorName;
        }
    }

    // Get motherboard make and model using win32 apis
    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"HARDWARE\\DESCRIPTION\\System\\BIOS", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        wchar_t biosName[256] = { 0 };
        DWORD biosNameSize = sizeof(biosName);
        if (RegQueryValueEx(hKey.get(), L"BaseBoardProduct", nullptr, nullptr, reinterpret_cast<BYTE*>(biosName), &biosNameSize) == ERROR_SUCCESS)
        {
            computerInfo.motherboard = biosName;
        }
    }

    // Get ram amount using GlobalMemoryStatusEx
    MEMORYSTATUSEX memoryStatus = { 0 };
    memoryStatus.dwLength = sizeof(memoryStatus);
    if (GlobalMemoryStatusEx(&memoryStatus))
    {
        computerInfo.ram = memoryStatus.ullTotalPhys / 1024 / 1024;
    }

    return computerInfo;
}

void UploadPerformanceDataTelemetry(std::chrono::milliseconds samplingPeriod, const std::span<ProcessPerformanceSummary>& data)
{
    using namespace std::chrono_literals;

    enum class UploadReason
    {
        None,
        MaxPercent,
        Sigma4,
        AveragePercent,
        SearchIndexer,
    };

    struct UploadItem
    {
        UploadReason reason;
        ProcessPerformanceSummary data;
    };

    constexpr auto c_quietSessionVersion = 1;

    // Calculate total cpu time usage for all processes
    std::chrono::microseconds totalCpuUsageInMicroseconds = std::accumulate(data.begin(), data.end(), 0us, [](std::chrono::microseconds total, const ProcessPerformanceSummary& item) {
        return total + std::chrono::microseconds(item.totalCpuTimeInMicroseconds);
    });

    // Begin metrics activity
    auto activity = DevHomeTelemetryProvider::QuietBackgroundProcesses_PerformanceMetrics::Start(
        c_quietSessionVersion,
        samplingPeriod.count(),
        totalCpuUsageInMicroseconds.count());

    // Upload computer information
    auto computerInformation = GetComputerInformation();
    activity.ComputerInfo(
        computerInformation.processorCount,
        computerInformation.processor.c_str(),
        computerInformation.motherboard.c_str(),
        computerInformation.ram);

    // Calculate the totalCpuTimeInMicroseconds items aggregated by item.category
    std::vector<uint64_t> numProcesses(5);
    std::vector<uint64_t> totalCpuTimesByCategory(5);
    for (const auto& item : data)
    {
        numProcesses[item.category]++;
        totalCpuTimesByCategory[item.category] += item.totalCpuTimeInMicroseconds;
    }

    // Upload category metrics
    activity.SessionCategoryMetrics(
        numProcesses[0],
        numProcesses[1],
        numProcesses[2],
        numProcesses[3],
        numProcesses[4],
        totalCpuTimesByCategory[0],
        totalCpuTimesByCategory[1],
        totalCpuTimesByCategory[2],
        totalCpuTimesByCategory[3],
        totalCpuTimesByCategory[4]);

    // Choose process information to upload
    std::vector<UploadItem> itemsToUpload;
    for (const auto& item : data)
    {
        if (item.maxPercent >= 20.0)
        {
            // Add item to list to upload
            itemsToUpload.emplace_back(UploadReason::MaxPercent, item);
        }
        else if (std::sqrt(std::sqrt(item.sigma4Cumulative / item.sampleCount)) >= 4.0)
        {
            // Add item to list to upload
            itemsToUpload.emplace_back(UploadReason::Sigma4, item);
        }
        else if (item.percentCumulative / item.sampleCount >= 1.0)
        {
            // Add item to list to upload
            itemsToUpload.emplace_back(UploadReason::AveragePercent, item);
        }
        else if (wil::compare_string_ordinal(item.name, L"SearchIndexer.exe", true) == 0)
        {
            // Add item to list to upload
            itemsToUpload.emplace_back(UploadReason::SearchIndexer, item);
        }
    }

    // Get system32 path
    wchar_t system32Path[MAX_PATH];
    GetSystemDirectory(system32Path, ARRAYSIZE(system32Path));

    // Upload process information
    for (const auto& itemToUpload : itemsToUpload)
    {
        activity.ProcessInfo(
            itemToUpload.reason,
            wil::compare_string_ordinal(itemToUpload.data.path, system32Path, true) == 0,
            itemToUpload.data.name,
            itemToUpload.data.category,
            itemToUpload.data.packageFullName,

            itemToUpload.data.sampleCount,
            itemToUpload.data.maxPercent,
            std::sqrt(std::sqrt(itemToUpload.data.sigma4Cumulative / itemToUpload.data.sampleCount)),
            itemToUpload.data.percentCumulative / itemToUpload.data.sampleCount,
            itemToUpload.data.totalCpuTimeInMicroseconds);
    }

    activity.Stop();
}
