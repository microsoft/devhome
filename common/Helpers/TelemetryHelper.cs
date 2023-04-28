// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Telemetry;

public static class TelemetryHelper
{
    public static void Log<T>(string eventName, LogLevel logLevel, T eventData)
        where T : EventBase
    {
        LoggerFactory.Get<ILogger>().Log(eventName, logLevel, eventData);
    }

    public static void LogError<T>(string eventName, LogLevel logLevel, T eventData)
        where T : EventBase
    {
        LoggerFactory.Get<ILogger>().LogError(eventName, logLevel, eventData);
    }

    //// Developer ID events

    public static void LogDeveloperIdStartup(string providerName, IEnumerable<IDeveloperId> devIds) =>
        LogAccountEvent("Startup_DevId_Event", providerName, devIds);

    public static void LogDeveloperIdLogIn(string providerName, IDeveloperId devId) =>
        LogAccountEvent("Login_DevId_Event", providerName, new IDeveloperId[] { devId });

    public static void LogDeveloperIdLogOut(string providerName, IDeveloperId devId) =>
        LogAccountEvent("Logout_DevId_Event", providerName, new IDeveloperId[] { devId });

    private static void LogAccountEvent(string eventName, string providerName, IEnumerable<IDeveloperId> devIds) =>
        Log(eventName, LogLevel.Critical, new DeveloperIdEvent(providerName, devIds));

    //// App Install events

    public static void LogAppSelectedForInstall(string packageId, string sourceId) =>
        LogAppInstallEvent("AppInstall_AppSelected", packageId, sourceId);

    public static void LogAppInstallSucceeded(string packageId, string sourceId) =>
        LogAppInstallEvent("AppInstall_InstallSucceeded", packageId, sourceId);

    private static void LogAppInstallEvent(string eventName, string packageId, string sourceId) =>
        Log(eventName, LogLevel.Critical, new AppInstallEvent(packageId, sourceId));

    public static void LogAppInstallFailed(string packageId, string sourceId) =>
        LogError("AppInstall_InstallFailed", LogLevel.Critical, new AppInstallEvent(packageId, sourceId));

    //// Dev Drive events

    /// <summary>
    /// Send Measure telemetry even indicating that a new Dev Drive creation operation was triggered.
    /// </summary>
    /// <param name="operationDuration">duration of the operation</param>
    /// <param name="hr"> HRESULT of the operation</param>
    /// <param name="sizeInBytes">Size of Dev Drive n bytes</param>
    /// <param name="mediaType">Volume media type</param>
    public static void LogCreateDevDriveTriggered(long operationDuration, int hr, ulong sizeInBytes, uint mediaType)
    {
        LoggerFactory.Get<ILogger>().Log("CreateDevDriveTriggered", LogLevel.Measure, new
        {
            PartA_PrivTags = PrivTags.ProductAndServiceUsage,
            duration = operationDuration,
            errorCode = $"0x{hr:X}",
            volumeSizeInBytes = sizeInBytes,
            diskMediaType = mediaType,
        });
    }

    /// <summary>
    /// Send Measure telemetry event indicating that user went to Disks and Volumes page in Settings
    /// </summary>
    /// <param name="eventSource">Event source (currently DevDriveView or MainPageView)</param>
    public static void LogLaunchDisksAndVolumesSettingsPageTriggered(string eventSource)
    {
        LoggerFactory.Get<ILogger>().Log("LaunchDisksAndVolumesSettingsPageTriggered", LogLevel.Measure, new
        {
            PartA_PrivTags = PrivTags.ProductAndServiceUsage,
            source = eventSource,
        });
    }
}
