// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management;
using Microsoft.Management.Infrastructure;
using Serilog;
using Windows.Win32.Foundation;

namespace HyperVExtension.CommunicationWithGuest;

// States based on InstallState value in Win32_OptionalFeature
// See: https://learn.microsoft.com/windows/win32/cimwin32prov/win32-optionalfeature
public enum FeatureAvailabilityKind
{
    Enabled,
    Disabled,
    Absent,
    Unknown,
}

internal sealed class WmiUtility
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WmiUtility));

    public enum ReturnCode : uint
    {
        Completed = 0,
        Started = 4096,
        Failed = 32768,
        AccessDenied = 32769,
        NotSupported = 32770,
        Unknown = 32771,
        Timeout = 32772,
        InvalidParameter = 32773,
        SystemInUse = 32774,
        InvalidState = 32775,
        IncorrectDataType = 32776,
        SystemNotAvailable = 32777,
        OutofMemory = 32778,
    }

    public enum JobState : ushort
    {
        New = 2,
        Starting = 3,
        Running = 4,
        Suspended = 5,
        ShuttingDown = 6,
        Completed = 7,
        Terminated = 8,
        Killed = 9,
        Exception = 10,
        Service = 11,
    }

    /// <summary>
    /// Common utility function to get a service object
    /// </summary>
    public static ManagementObject GetServiceObject(ManagementScope scope, string serviceName)
    {
        scope.Connect();
        ManagementPath wmiPath = new ManagementPath(serviceName);
        ManagementClass serviceClass = new ManagementClass(scope, wmiPath, null);
        ManagementObjectCollection services = serviceClass.GetInstances();

        if (services.Count == 0)
        {
            throw new System.ComponentModel.Win32Exception((int)WIN32_ERROR.ERROR_NOT_FOUND, $"Cannot instantiate '{serviceName}'.");
        }

        ManagementObject? serviceObject = null;

        foreach (ManagementObject service in services)
        {
            serviceObject = service;
        }

        return serviceObject!;
    }

    public static ManagementObject GetTargetComputer(Guid vmId, ManagementScope scope)
    {
        var query = $"select * from Msvm_ComputerSystem Where Name = '{vmId.ToString("D")}'";

        ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));

        ManagementObjectCollection computers = searcher.Get();

        if (computers.Count == 0)
        {
            throw new System.ComponentModel.Win32Exception((int)WIN32_ERROR.ERROR_NOT_FOUND, $"Cannot find target computer '{vmId.ToString("D")}'.");
        }

        ManagementObject? computer = null;

        foreach (ManagementObject instance in computers)
        {
            computer = instance;
            break;
        }

        return computer!;
    }

    public static bool JobCompleted(ManagementBaseObject outParams, ManagementScope scope, out uint errorCode, out string errorDescription)
    {
        errorCode = 0;
        errorDescription = string.Empty;

        // Retrieve msvc_StorageJob path. This is a full wmi path
        var jobPath = (string)outParams["Job"];
        ManagementObject job = new ManagementObject(scope, new ManagementPath(jobPath), null);

        // Try to get storage job information
        var log = Log.ForContext("SourceContext", nameof(WmiUtility));
        job.Get();
        while ((ushort)job["JobState"] == (ushort)JobState.Starting
            || (ushort)job["JobState"] == (ushort)JobState.Running)
        {
            log.Information($"WMI job in progress... {job["PercentComplete"]}% completed.");
            Thread.Sleep(300);
            job.Get();
        }

        // Figure out if job failed
        var jobCompleted = true;
        var jobState = (ushort)job["JobState"];
        if (jobState != (ushort)JobState.Completed)
        {
            errorCode = (ushort)job["ErrorCode"];
            errorDescription = (string)job["ErrorDescription"];
            log.Error($"WMI job state: {jobState}.");
            log.Error($"WMI job error: {errorCode}.");
            log.Error($"WMI job error description: {errorDescription}.");
            jobCompleted = false;
        }

        return jobCompleted;
    }

    public static FeatureAvailabilityKind GetHyperVFeatureAvailability()
    {
        try
        {
            var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_OptionalFeature WHERE Name = 'Microsoft-Hyper-V'");
            var collection = searcher.Get();

            foreach (var instance in collection)
            {
                if (instance?.GetPropertyValue("InstallState") is uint enablementState)
                {
                    var featureAvailability = GetAvailabilityKindFromState(enablementState);

                    _log.Information($"Found Hyper-V feature with enablement state: '{featureAvailability}'");
                    return featureAvailability;
                }
            }
        }
        catch (Exception ex)
        {
            // We'll handle cases where there are exceptions as if the feature does not exist.
            _log.Error(ex, $"Error attempting to get the Hyper-V feature state");
        }

        _log.Information($"Unable to find Hyper-V feature");
        return FeatureAvailabilityKind.Unknown;
    }

    private static FeatureAvailabilityKind GetAvailabilityKindFromState(uint state)
    {
        switch (state)
        {
            case 1:
                return FeatureAvailabilityKind.Enabled;
            case 2:
                return FeatureAvailabilityKind.Disabled;
            case 3:
                return FeatureAvailabilityKind.Absent;
            default:
                return FeatureAvailabilityKind.Unknown;
        }
    }
}
