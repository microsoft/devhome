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

    BEGIN_COMPLIANT_MEASURES_ACTIVITY_CLASS_WITH_LEVEL(
        QuietBackgroundProcesses_ElevatedServer_Session,
        PDT_ProductAndServicePerformance,
        WINEVENT_LEVEL_CRITICAL);

        DEFINE_ACTIVITY_START(uint64_t expectedDuration)
        {
            TraceLoggingClassWriteStart(
                QuietBackgroundProcesses_ElevatedServer_Session,
                TraceLoggingValue(expectedDuration, "ExpectedDuration"));
        }
        DEFINE_ACTIVITY_STOP(bool manuallyStopped, uint64_t actualDuration)
        {
            TraceLoggingClassWriteStop(
                QuietBackgroundProcesses_ElevatedServer_Session,
                TraceLoggingValue(manuallyStopped, "ManuallyStopped"),
                TraceLoggingValue(actualDuration, "ActualDuration"));
        }
    END_ACTIVITY_CLASS();
};
