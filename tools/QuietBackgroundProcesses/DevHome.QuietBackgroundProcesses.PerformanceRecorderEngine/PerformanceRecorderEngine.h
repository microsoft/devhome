// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <appmodel.h>

struct ProcessPerformanceSummary
{
    // Process info
    ULONG pid{};
    wchar_t name[64];
    wchar_t packageFullName[PACKAGE_FULL_NAME_MAX_LENGTH + 1]{};
    wchar_t aumid[APPLICATION_USER_MODEL_ID_MAX_LENGTH]{};
    wchar_t path[MAX_PATH * 2]{};
    uint32_t category{};
    FILETIME createTime{};
    FILETIME exitTime{};

    // Sampling
    uint64_t sampleCount{};
    double percentCumulative{};
    double varianceCumulative{};
    double sigma4Cumulative{};
    double maxPercent{};
    uint32_t samplesAboveThreshold{};

    // Other
    uint64_t totalCpuTimeInMicroseconds{};
};

extern "C" __declspec(dllexport) HRESULT StartMonitoringProcessUtilization(uint32_t periodInMs, void** context) noexcept;
extern "C" __declspec(dllexport) HRESULT StopMonitoringProcessUtilization(void* context) noexcept;
extern "C" __declspec(dllexport) HRESULT GetMonitoringProcessUtilization(void* context, ProcessPerformanceSummary** ppSummaries, size_t* summaryCount) noexcept;
extern "C" __declspec(dllexport) HRESULT DeleteMonitoringProcessUtilization(void* context) noexcept;

using unique_process_utilization_monitoring_thread = wil::unique_any<void*, decltype(&::DeleteMonitoringProcessUtilization), ::DeleteMonitoringProcessUtilization>;
