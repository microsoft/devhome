// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Helpers;

public static class WindowsOptionalFeatureNames
{
    public const string GuardedHost = "HostGuardian";
    public const string HyperV = "Microsoft-Hyper-V-All";
    public const string HyperVManagementTools = "Microsoft-Hyper-V-Tools-All";
    public const string HyperVPlatform = "Microsoft-Hyper-V";
    public const string VirtualMachinePlatform = "VirtualMachinePlatform";
    public const string WindowsHypervisorPlatform = "HypervisorPlatform";
    public const string WindowsSandbox = "Containers-DisposableClientVM";
    public const string WindowsSubsystemForLinux = "Microsoft-Windows-Subsystem-Linux";

    public static IEnumerable<string> VirtualMachineFeatures => new[]
    {
        GuardedHost,
        HyperV,
        HyperVManagementTools,
        HyperVPlatform,
        VirtualMachinePlatform,
        WindowsHypervisorPlatform,
        WindowsSandbox,
        WindowsSubsystemForLinux,
    };
}
