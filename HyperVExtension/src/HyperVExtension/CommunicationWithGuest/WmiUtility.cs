// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management;
using System.Management.Automation;
using System.Web.Services.Description;
using HyperVExtension.Providers;
using Windows.Win32.Foundation;

namespace HyperVExtension.CommunicationWithGuest;

internal sealed class WmiUtility
{
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
        job.Get();
        while ((ushort)job["JobState"] == (ushort)JobState.Starting
            || (ushort)job["JobState"] == (ushort)JobState.Running)
        {
            Logging.Logger()?.ReportInfo($"WMI job in progress... {job["PercentComplete"]}% completed.");
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
            Logging.Logger()?.ReportError($"WMI job state: {jobState}.");
            Logging.Logger()?.ReportError($"WMI job error: {errorCode}.");
            Logging.Logger()?.ReportError($"WMI job error description: {errorDescription}.");
            jobCompleted = false;
        }

        return jobCompleted;
    }
}
