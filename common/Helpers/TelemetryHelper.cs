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

    public static void LogDeveloperIdStartup(string providerName, IEnumerable<IDeveloperId> devIds) =>
        LogAccountEvent("Startup_DevId_Event", providerName, devIds);

    public static void LogDeveloperIdLogIn(string providerName, IDeveloperId devId) =>
        LogAccountEvent("Login_DevId_Event", providerName, new IDeveloperId[] { devId });

    public static void LogDeveloperIdLogOut(string providerName, IDeveloperId devId) =>
        LogAccountEvent("Logout_DevId_Event", providerName, new IDeveloperId[] { devId });

    private static void LogAccountEvent(string eventName, string providerName, IEnumerable<IDeveloperId> devIds) =>
        Log(eventName, LogLevel.Critical, new DeveloperIdEvent(providerName, devIds));

    public static void LogAppSelectedForInstall(string packageId, string sourceId) =>
        LogAppInstallEvent("AppInstall_AppSelected", packageId, sourceId);

    public static void LogAppInstallSucceeded(string packageId, string sourceId) =>
        LogAppInstallEvent("AppInstall_InstallSucceeded", packageId, sourceId);

    public static void LogAppInstallFailed(string packageId, string sourceId) =>
        LogAppInstallEvent("AppInstall_InstallFailed", packageId, sourceId);

    private static void LogAppInstallEvent(string eventName, string packageId, string sourceId) =>
        Log(eventName, LogLevel.Critical, new AppInstallEvent(packageId, sourceId));
}
