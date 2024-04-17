// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "pch.h"

#include <chrono>
#include <filesystem>
#include <iostream>
#include <map>
#include <mutex>
#include <set>
#include <span>
#include <string>
#include <string_view>
#include <thread>
#include <vector>

#include <wil/resource.h>
#include <wil/token_helpers.h>
#include <wil/win32_helpers.h>

#include <windows.h>
#include <appmodel.h>
#include <psapi.h>

#include "PerformanceRecorderEngine.h"

// 2 percent is the threshold for a process to be considered as a high CPU consumer
#define CPU_TIME_ABOVE_THRESHOLD_STRIKE_VALUE 0.02f

enum class ProcessCategory
{
    Unknown,
    User,
    System,
    Developer,
    Background,
};

struct ProcessPerformanceInfo
{
    // Process id
    wil::unique_process_handle process;
    ULONG pid{};

    // Process info
    std::wstring name;
    std::wstring path;
    std::optional<std::wstring> packageFullName;
    std::optional<std::wstring> aumid;
    ProcessCategory category{};
    FILETIME createTime{};
    FILETIME exitTime{};

    // CPU times
    FILETIME startUserTime{};
    FILETIME startKernelTime{};
    FILETIME previousUserTime{};
    FILETIME currentUserTime{};
    FILETIME previousKernelTime{};
    FILETIME currentKernelTime{};

    // Sampling
    uint64_t sampleCount{};
    double percentCumulative{};
    double varianceCumulative{};
    double sigma4Cumulative{};
    double maxPercent{};
    uint32_t samplesAboveThreshold{};
};

// Process Categories
std::set<std::wstring> c_user = {
    L"chrome.exe",
    L"OUTLOOK.exe",
    L"EXCEL.exe",
    L"explorer.exe",
    L"WINWORD.exe",
    L"POWERPOINT.exe",
    L"OfficeClickToRun.exe",
    L"Microsoft.SharePoint.exe",
    L"msedge.exe",
    L"msedgewebview2.exe",
    L"ShellExperienceHost.exe",
    L"StartMenuExperienceHost.exe",
    L"smartscreen.exe",
    L"sihost.exe",
    L"SystemSettings.exe",
    L"electron.exe",
    L"CrmSandbox.exe",
    L"ms-teams.exe",
    L"TextInputHost.exe",
    L"UserOOBEBroker.exe",
    L"WebViewHost.exe",
    L"Widgets.exe",
    L"WidgetService.exe",
    L"XboxGameBarWidgets.exe",
    L"teams.exe"
};
std::set<std::wstring> c_system = {
    L"System",
    L"Registry",
    L"Secure System",
    L"audiodg.exe",
    L"ctfmon.exe",
    L"LogonUI.exe",
    L"MpDefenderCoreService.exe",
    L"MpDlpService.exe",
    L"ShellHost.exe",
    L"smss.exe",
    L"spoolsv.exe",
    L"wininit.exe",
    L"lsass.exe"
};
std::set<std::wstring> c_developer = {
    L"cmd.exe",
    L"conhost.exe",
    L"console.exe",
    L"OpenConsole.exe",
    L"powershell.exe",
    L"cl.exe",
    L"link.exe",
    L"devenv.exe",
    L"DevHome.exe",
    L"DevHomeGitHubExtension.exe",
    L"python.exe",
    L"build.exe",
    L"msbuild.exe",
    L"windbg.exe",
    L"windbgx.exe",
    L"EngHost.exe",
    L"DbgX.Shell.exe",
    L"GVFS.Mount.exe",
    L"GVFS.Service.exe",
    L"GVFS.ServiceUI.exe",
    L"vscode.exe",
    L"code.exe",
    L"cpptools.exe",
    L"notepad.exe",
    L"notepad++.exe",
    L"Wex.Services.exe",
    L"Taskmgr.exe",
    L"wpa.exe",
    L"wpr.exe",
    L"CalculatorApp.exe",
    L"npm.exe",
    L"winget.exe",
    L"chocolatey.exe",
    L"pip.exe",
    L"vshost.exe",
    L"VSSVC.exe",
    L"VBCSCompiler.exe",
    L"vcpkgsrv.exe",
    L"WindowsTerminal.exe",
    L"WindowsPackageManagerServer.exe",
    L"reSearch.exe"
};
std::set<std::wstring> c_vms = {
    L"vmmem",
    L"vmwp.exe",  // actual process name for 'vmmem'
    L"vmcompute.exe",
    L"vmconnect.exe",
    L"vmwp.exe",
    L"vmms.exe"
};
std::set<std::wstring> c_background = {
    L"services.exe",
    L"svchost.exe",
    L"SCNotification.exe",
    L"SecurityHealthyService.exe",
    L"DevHome.QuietBackgroundProcesses.Server.exe",
    L"DevHome.QuietBackgroundProcesses.ElevatedServer.exe",
    L"OneDrive.exe",
    L"MsMpEng.exe",
    L"MsSense.exe",
    L"NdrSetup.exe",
    L"NisSrv.exe",
    L"RuntimeBroker.exe",
    L"rundll32.exe",
    L"SearchHost.exe",
    L"SenseCE.exe",
    L"SenseNdr.exe",
    L"SenseNdrX.exe",
    L"SenseTVM.exe",
    L"SearchIndexer.exe",
    L"taskhostw.exe",
    L"winlogon.exe",
};

ProcessCategory GetCategory(DWORD pid, std::wstring_view processName)
{
    auto search = [&](std::wstring_view processName, const auto& list)
    {
        auto it = std::find_if(list.begin(), list.end(), [&](const auto& elem)
        {
            return wil::compare_string_ordinal(processName, elem, true) == 0;
        });
        auto found = (it != list.end());
        return found;
    };

    if (pid == 4)
    {
        // PID 4 is the System process
        return ProcessCategory::System;
    }
    if (search(processName.data(), c_user))
    {
        return ProcessCategory::User;
    }
    if (search(processName.data(), c_system))
    {
        return ProcessCategory::System;
    }
    if (search(processName.data(), c_developer))
    {
        return ProcessCategory::Developer;
    }
    if (search(processName.data(), c_vms))
    {
        return ProcessCategory::Developer;
    }
    if (search(processName.data(), c_background))
    {
        return ProcessCategory::Background;
    }
    return ProcessCategory::Unknown;
}

template <size_t N>
void copystr(wchar_t(&dst)[N], const std::optional<std::wstring>& src)
{
    wcscpy_s(dst, N, src.value_or(L"").substr(0, N - 1).c_str());
}

template<typename T>
wil::unique_cotaskmem_array_ptr<T> make_unique_cotaskmem_array_ptr(size_t numOfElements)
{
    wil::unique_cotaskmem_array_ptr<T> result;
    T* ptr = reinterpret_cast<T*>(CoTaskMemAlloc(sizeof(T) * numOfElements));
    THROW_IF_NULL_ALLOC(ptr);
    *result.addressof() = ptr;
    *result.size_address() = numOfElements;
    return result;
}

std::chrono::file_clock::time_point FileTimeToTimePoint(const FILETIME& fileTime)
{
    ULARGE_INTEGER uli;
    uli.LowPart = fileTime.dwLowDateTime;
    uli.HighPart = fileTime.dwHighDateTime;
    std::chrono::file_clock::duration d{ (static_cast<int64_t>(fileTime.dwHighDateTime) << 32) | fileTime.dwLowDateTime };
    std::chrono::file_clock::time_point tp{ d };
    return tp;
}

std::string FiletimeToString(const FILETIME& ft)
{
    std::chrono::file_clock::duration d{ (static_cast<int64_t>(ft.dwHighDateTime) << 32) | ft.dwLowDateTime };
    std::chrono::file_clock::time_point tp{ d };
    return std::format("{:%Y-%m-%d %H:%M}\n", tp);
}

std::chrono::microseconds CpuTimeDuration(FILETIME previous, FILETIME current)
{
    if (CompareFileTime(&previous, &current) >= 0)
    {
        return std::chrono::microseconds(0);
    }

    auto filetimeDeltaIn100ns = FileTimeToTimePoint(current) - FileTimeToTimePoint(previous);
    auto durationMicroseconds = std::chrono::duration_cast<std::chrono::microseconds>(filetimeDeltaIn100ns);
    return durationMicroseconds;
}

int GetVirtualNumCpus()
{
    SYSTEM_INFO sysInfo;
    GetSystemInfo(&sysInfo);
    return sysInfo.dwNumberOfProcessors;
}

template<std::size_t size>
std::span<DWORD> GetPids(DWORD (&pidArray)[size])
{
    DWORD needed;
    THROW_IF_WIN32_BOOL_FALSE(EnumProcesses(pidArray, sizeof(pidArray), &needed));
    return { &pidArray[0], needed / sizeof(DWORD) };
}

std::optional<std::wstring> TryGetPackageFullNameFromTokenHelper(HANDLE token)
{
    wchar_t packageFullName[PACKAGE_FULL_NAME_MAX_LENGTH + 1]{};
    uint32_t packageFullNameLength = ARRAYSIZE(packageFullName);
    if (GetPackageFullNameFromToken(token, &packageFullNameLength, packageFullName))
    {
        return std::nullopt;
    }
    return std::wstring { packageFullName };
}

std::optional<std::wstring> TryGetAppUserModelIdFromTokenHelper(HANDLE token)
{
    wchar_t aumid[APPLICATION_USER_MODEL_ID_MAX_LENGTH]{};
    uint32_t aumidLength = ARRAYSIZE(aumid);
    if (GetApplicationUserModelIdFromToken(token, &aumidLength, aumid) != ERROR_SUCCESS)
    {
        return std::nullopt;
    }
    return std::wstring { aumid };
}

std::optional<std::wstring> TryGetProcessName(HANDLE processHandle)
{
    static wchar_t s_buffer[MAX_PATH * 2];
    if (GetModuleFileNameExW(processHandle, nullptr, s_buffer, _countof(s_buffer)) > 0)
    {
        return s_buffer;
    }
    return std::nullopt;
}

ProcessPerformanceInfo MakeProcessPerformanceInfo(DWORD processId)
{
    auto process = wil::unique_process_handle{ OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId) };
    if (!process)
    {
        // We can't open csrss.exe, so we'll just skip processes we can't open
        auto info = ProcessPerformanceInfo{};
        info.process = nullptr;
        info.pid = processId;
    }

    auto processPathString = TryGetProcessName(process.get());

    auto path = std::filesystem::path(processPathString.value_or(L""));

    FILETIME createTime, exitTime, kernelTime, userTime;
    THROW_IF_WIN32_BOOL_FALSE(GetProcessTimes(process.get(), &createTime, &exitTime, &kernelTime, &userTime));

    std::optional<std::wstring> packageFullName;
    std::optional<std::wstring> aumid;
    wil::unique_handle processToken;
    if (OpenProcessToken(process.get(), TOKEN_QUERY, &processToken))
    {
        packageFullName = TryGetPackageFullNameFromTokenHelper(processToken.get());
        aumid = TryGetAppUserModelIdFromTokenHelper(processToken.get());
    }

    auto info = ProcessPerformanceInfo{};
    info.process = std::move(process);
    info.pid = processId;
    info.name = path.filename().wstring();
    info.path = path.parent_path().wstring();
    info.packageFullName = packageFullName;
    info.aumid = aumid;
    info.category = GetCategory(info.pid, info.name);
    info.createTime = createTime;

    // Start times
    info.startUserTime = userTime;
    info.startKernelTime = kernelTime;

    info.previousUserTime = userTime;
    info.currentUserTime = userTime;
    info.previousKernelTime = kernelTime;
    info.currentKernelTime = kernelTime;

    return info;
}

bool UpdateProcessPerformanceInfo(ProcessPerformanceInfo& info)
{
    FILETIME createTime, exitTime, kernelTime, userTime;
    THROW_IF_WIN32_BOOL_FALSE(GetProcessTimes(info.process.get(), &createTime, &exitTime, &kernelTime, &userTime));

    if (exitTime.dwHighDateTime != 0 || exitTime.dwLowDateTime != 0)
    {
        info.exitTime = info.exitTime;
        return false;
    }

    info.previousUserTime = info.currentUserTime;
    info.currentUserTime = userTime;
    info.previousKernelTime = info.currentKernelTime;
    info.currentKernelTime = kernelTime;
    return true;
}

struct cancellation_mechanism
{
    std::atomic<bool> m_cancelled{};
    std::mutex m_mutex;
    std::condition_variable m_cancelCondition;

    void cancel()
    {
        auto lock = std::scoped_lock(m_mutex);
        m_cancelled = true;
        m_cancelCondition.notify_all();
    }

    bool wait_for_cancel(std::chrono::milliseconds duration)
    {
        auto lock = std::unique_lock<std::mutex>(m_mutex);
        auto cancelHappened = m_cancelCondition.wait_for(lock, duration, [this] {
            return m_cancelled.load();
        });
        return cancelHappened;
    }
};

struct MonitorThread
{
    cancellation_mechanism m_cancellationMechanism;
    std::thread m_thread;
    std::mutex m_dataMutex;

    // Tracking all our process infos
    std::map<ULONG, ProcessPerformanceInfo> m_runningProcesses;
    std::vector<ProcessPerformanceInfo> m_terminatedProcesses;

    MonitorThread(std::chrono::milliseconds periodMs)
    {
        if (periodMs.count() <= 0)
        {
            THROW_HR(E_INVALIDARG);
        }

        m_thread = std::thread([this, periodMs]() {
            try
            {
                auto numCpus = GetVirtualNumCpus();

                while (true)
                {
                    if (m_cancellationMechanism.m_cancelled)
                    {
                        break;
                    }

                    std::chrono::microseconds totalMicroseconds{};

                    // Check for new processes to track
                    DWORD pidArray[2048];
                    auto pids = GetPids(pidArray);

                    auto lock = std::scoped_lock(m_dataMutex);
                    for (auto& pid : pids)
                    {
                        // Ignore process "0" - the 'SYSTEM 'System' process
                        if (pid == 0)
                        {
                            continue;
                        }

                        // Make a new entry
                        if (!m_runningProcesses.contains(pid))
                        {
                            try
                            {
                                m_runningProcesses[pid] = MakeProcessPerformanceInfo(pid);
                            }
                            CATCH_LOG();
                        }
                    }

                    // Update counts for each tracked process
                    for (auto it = m_runningProcesses.begin(); it != m_runningProcesses.end(); )
                    {
                        auto pid = it->first;

                        // Get entry
                        auto& info = it->second;

                        if (!info.process)
                        {
                            // The process couldn't be opened, so we'll skip this entry
                            ++it;
                            continue;
                        }

                        // Update entry
                        try
                        {
                            if (!UpdateProcessPerformanceInfo(info))
                            {
                                // The process terminated

                                // Destroy the process handle
                                info.process.reset();

                                // Move from the map to the terminated list
                                m_terminatedProcesses.push_back(std::move(info));
                                it = m_runningProcesses.erase(it);
                                continue;
                            }
                        }
                        catch (...)
                        {
                            ++it;
                            continue;
                        }

                        // Collect cpuTime for process
                        auto cpuTime = CpuTimeDuration(info.previousUserTime, info.currentUserTime);
                        cpuTime += CpuTimeDuration(info.previousKernelTime, info.currentKernelTime);

                        double percent = (double)cpuTime.count() / std::chrono::duration_cast<std::chrono::microseconds>(periodMs).count() / (double)numCpus * 100.0f;
                        double variance = (double)std::pow(percent, 2.0f);
                        double sigma4 = (double)std::pow(percent, 4.0f);

                        info.sampleCount++;
                        info.percentCumulative += percent;
                        info.varianceCumulative += variance;
                        info.sigma4Cumulative += sigma4;
                        if (percent > info.maxPercent)
                        {
                            info.maxPercent = percent;
                        }
                        if (percent > CPU_TIME_ABOVE_THRESHOLD_STRIKE_VALUE)
                        {
                            info.samplesAboveThreshold++;
                        }

                        totalMicroseconds += cpuTime;

                        ++it;
                    }

                    // Wait for interval period or user cancellation
                    if (m_cancellationMechanism.wait_for_cancel(periodMs))
                    {
                        // User cancelled
                        break;
                    }
                }
            }
            CATCH_LOG();
        });
    }

    void Cancel()
    {
        m_cancellationMechanism.cancel();
        if (m_thread.joinable())
        {
            m_thread.join();
        }
    }

    std::vector<ProcessPerformanceSummary> GetProcessPerformanceSummaries()
    {
        auto lock = std::scoped_lock(m_dataMutex);

        std::vector<ProcessPerformanceSummary> summaries;
        auto MakeSummary = [](const ProcessPerformanceInfo& info)
        {
            auto summary = ProcessPerformanceSummary{};
            auto totalUserTime = CpuTimeDuration(info.startUserTime, info.currentUserTime);
            auto totalKernelTime = CpuTimeDuration(info.startKernelTime, info.currentKernelTime);

            // Process info
            summary.pid = info.pid;
            if (summary.pid == 4)
            {
                copystr(summary.name, L"[System]");
            }
            else if (info.name.empty())
            {
                copystr(summary.name, L"[unk]");
            }
            else
            {
                copystr(summary.name, info.name);
            }
            copystr(summary.packageFullName, info.packageFullName);
            copystr(summary.aumid, info.aumid);
            copystr(summary.path, info.path);
            summary.category = static_cast<uint32_t>(info.category);
            summary.createTime = info.createTime;
            summary.exitTime = info.exitTime;

            // Sampling
            summary.sampleCount = info.sampleCount;
            summary.percentCumulative = info.percentCumulative;
            summary.varianceCumulative = info.varianceCumulative;
            summary.sigma4Cumulative = info.sigma4Cumulative;
            summary.maxPercent = info.maxPercent;
            summary.samplesAboveThreshold = info.samplesAboveThreshold;

            // Other
            summary.totalCpuTimeInMicroseconds = totalUserTime.count() + totalKernelTime.count();

            if (summary.sampleCount <= 0)
            {
                summary.sampleCount = 0;
            }
            return summary;
        };

        // Add summaries for running processes
        for (auto const& [key, info] : m_runningProcesses)
        {
            summaries.push_back(MakeSummary(info));
        }

        // Add summaries for terminated processes
        for (auto const& info : m_terminatedProcesses)
        {
            summaries.push_back(MakeSummary(info));
        }
        return summaries;
    }
};

//
// Exports
//

extern "C" __declspec(dllexport) HRESULT StartMonitoringProcessUtilization(uint32_t periodInMs, void** context) noexcept
try
{
    auto periodMs = std::chrono::milliseconds(periodInMs);
    auto monitorThread = std::make_unique<MonitorThread>(periodMs);
    *context = static_cast<void*>(monitorThread.release());
    return S_OK;
}
CATCH_RETURN()

extern "C" __declspec(dllexport) HRESULT StopMonitoringProcessUtilization(void* context) noexcept
try
{
    auto monitorThread = reinterpret_cast<MonitorThread*>(context);
    monitorThread->Cancel();
    return S_OK;
}
CATCH_RETURN()

extern "C" __declspec(dllexport) HRESULT DeleteMonitoringProcessUtilization(void* context) noexcept
try
{
    if (!context)
    {
        return S_OK;
    }
    auto monitorThread = std::unique_ptr<MonitorThread>(reinterpret_cast<MonitorThread*>(context));
    monitorThread->Cancel();
    monitorThread.reset();
    return S_OK;
}
CATCH_RETURN()

extern "C" __declspec(dllexport) HRESULT GetMonitoringProcessUtilization(void* context, ProcessPerformanceSummary** ppSummaries, size_t* summaryCount) noexcept
try
{
    auto monitorThread = reinterpret_cast<MonitorThread*>(context);
    auto summaries = monitorThread->GetProcessPerformanceSummaries();

    // Alloc summaries block
    auto ptrSummaries = make_unique_cotaskmem_array_ptr<ProcessPerformanceSummary>(summaries.size());
    auto i = 0;
    for (auto const& summary : summaries)
    {
        auto& dst = ptrSummaries.get()[i++];
        dst = summary;
    }

    *summaryCount = ptrSummaries.size();
    *ppSummaries = ptrSummaries.release();

    return S_OK;
}
CATCH_RETURN()
