// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Telemetry;

public static class TelemetryHelper
{
    public static void Log(string eventName, LogLevel logLevel, EventBase eventData)
    {
        LoggerFactory.Get<ILogger>().Log(eventName, logLevel, eventData);
    }

    public static void LogAccountEvent(string eventName, string providerName, IDeveloperId devId)
    {
        LogAccountEvent(eventName, providerName, new IDeveloperId[] { devId });
    }

    public static void LogAccountEvent(string eventName, string providerName, IEnumerable<IDeveloperId> devIds)
    {
        Log(eventName, LogLevel.Critical, new DeveloperIdEvent(providerName, devIds));
    }
}
