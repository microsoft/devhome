// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

//#include <TraceLogging.h>
#include <TraceLoggingProvider.h>
#include <MicrosoftTelemetry.h>

#define __WIL_TRACELOGGING_CONFIG_H
#include <wil/Tracelogging.h>

// [uuid(2e74ff65-bbda-5e80-4c0a-bd8320d4223b)]
class DevHomeTelemetryProvider : public wil::TraceLoggingProvider
{
    IMPLEMENT_TRACELOGGING_CLASS(DevHomeTelemetryProvider, "Microsoft.Windows.DevHome", (0x2e74ff65, 0xbbda, 0x5e80, 0x4c, 0x0a, 0xbd, 0x83, 0x20, 0xd4, 0x22, 0x3b));

public:

    // Activity for quiet session
    BEGIN_COMPLIANT_MEASURES_ACTIVITY_CLASS_WITH_LEVEL(
        QuietBackgroundProcesses_ElevatedServer_Session,
        PDT_ProductAndServicePerformance,
        WINEVENT_LEVEL_CRITICAL);

        DEFINE_ACTIVITY_START(bool isPlacebo, uint64_t expectedDuration)
        {
            TraceLoggingClassWriteStart(
                QuietBackgroundProcesses_ElevatedServer_Session,
                TraceLoggingValue(isPlacebo, "isPlacebo"),
                TraceLoggingValue(expectedDuration, "expectedDuration"));
        }
        DEFINE_ACTIVITY_STOP(bool manuallyStopped, uint64_t actualDuration)
        {
            TraceLoggingClassWriteStop(
                QuietBackgroundProcesses_ElevatedServer_Session,
                TraceLoggingValue(manuallyStopped, "manuallyStopped"),
                TraceLoggingValue(actualDuration, "actualDuration"));
        }
    END_ACTIVITY_CLASS();


    // Activity for process metrics
    BEGIN_COMPLIANT_MEASURES_ACTIVITY_CLASS_WITH_LEVEL(
        QuietBackgroundProcesses_PerformanceMetrics,
        PDT_ProductAndServicePerformance,
        WINEVENT_LEVEL_CRITICAL);

        DEFINE_ACTIVITY_START(uint32_t quietSessionVersion, uint64_t samplingPeriodInMs, uint64_t totalCpuUsageInMicroseconds)
        {
            TraceLoggingClassWriteStart(QuietBackgroundProcesses_PerformanceMetrics,
                TraceLoggingValue(quietSessionVersion, "quietSessionVersion"),
                TraceLoggingValue(samplingPeriodInMs, "samplingPeriodInMs"),
                TraceLoggingValue(totalCpuUsageInMicroseconds, "totalCpuUsageInMicroseconds")
            );
        }

        DEFINE_TRACELOGGING_EVENT_PARAM4(
            ComputerInfo,
            DWORD, processorCount,
            PCWSTR, processor,
            PCWSTR, motherboard,
            DWORD, ram);

        DEFINE_TRACELOGGING_EVENT_PARAM10(
            SessionCategoryMetrics,
            int, numProcesses_unknown,
            int, numProcesses_user,
            int, numProcesses_system,
            int, numProcesses_developer,
            int, numProcesses_background,
            int, totalCpuTimesByCategory_unknown,
            int, totalCpuTimesByCategory_user,
            int, totalCpuTimesByCategory_system,
            int, totalCpuTimesByCategory_developer,
            int, totalCpuTimesByCategory_background);

        DEFINE_TRACELOGGING_EVENT_PARAM10(
            ProcessInfo,
            int, reason,
            bool, isInSystem32,
            PCWSTR, processName,
            uint32_t, category,
            PCWSTR, packageFullName,
            int, sampleCount,
            double, maxPercent,
            double, sigma4,
            double, percent,
            int, totalCpuTimeInMicroseconds);
    END_ACTIVITY_CLASS();
};
