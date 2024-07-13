// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Helpers;

public static class HyperVStrings
{
    // Common strings used for Hyper-V extension
    public const string HyperVModuleName = "Hyper-V";
    public const string HyperVProviderDisplayName = "Microsoft Hyper-V";
    public const string HyperVProviderId = "Microsoft.HyperV";
    public const string Name = "Name";
    public const string VMManagementService = "vmms"; // virtual machine management service.

    // see: https://learn.microsoft.com/windows-server/identity/ad-ds/manage/understand-security-identifiers
    public const string HyperVAdminGroupWellKnownSid = "S-1-5-32-578";

    // Hyper-V VM member strings
    public const string ComputerName = "ComputerName";
    public const string CPUUsage = "CPUUsage";
    public const string CreationTime = "CreationTime";
    public const string DynamicMemoryEnabled = "DynamicMemoryEnabled";
    public const string HardDrives = "HardDrives";
    public const string Id = "Id";
    public const string IsDeleted = "IsDeleted";
    public const string MemoryAssigned = "MemoryAssigned";
    public const string MemoryDemand = "MemoryDemand";
    public const string MemoryMaximum = "MemoryMaximum";
    public const string MemoryMinimum = "MemoryMinimum";
    public const string MemoryStartup = "MemoryStartup";
    public const string MemoryStatus = "MemoryStatus";
    public const string ParentCheckpointId = "ParentCheckpointId";
    public const string ParentCheckpointName = "ParentCheckpointName";
    public const string Path = "Path";
    public const string ProcessorCount = "ProcessorCount";
    public const string State = "State";
    public const string Status = "Status";
    public const string Uptime = "Uptime";
    public const string VmId = "VMId";
    public const string VMName = "VMName";
    public const string VMSnapshotId = "VMSnapshotId";
    public const string VMSnapshotName = "VMSnapshotName";
    public const string Size = "Size";
    public const string VirtualMachinePath = "VirtualMachinePath";
    public const string VirtualHardDiskPath = "VirtualHardDiskPath";
    public const string LogicalProcessorCount = "LogicalProcessorCount";

    // Hyper-V PowerShell commands strings
    public const string GetModule = "Get-Module";
    public const string SelectObject = "Select-Object";
    public const string StartService = "Start-Service";
    public const string GetService = "Get-Service";
    public const string GetVM = "Get-VM";
    public const string GetVHD = "Get-VHD";
    public const string StartVM = "Start-VM";
    public const string StopVM = "Stop-VM";
    public const string SuspendVM = "Suspend-VM";
    public const string ResumeVM = "Resume-VM";
    public const string RemoveVM = "Remove-VM";
    public const string GetVMSnapshot = "Get-VMSnapshot";
    public const string RestoreVMSnapshot = "Restore-VMSnapshot";
    public const string RemoveVMSnapshot = "Remove-VMSnapshot";
    public const string CreateVMCheckpoint = "Checkpoint-VM";
    public const string RestartVM = "Restart-VM";
    public const string GetVMHost = "Get-VMHost";
    public const string NewVM = "New-VM";
    public const string SetVM = "Set-VM";
    public const string SetVMFirmware = "Set-VMFirmware";

    // Hyper-V PowerShell command parameter strings
    public const string ListAvailable = "ListAvailable";
    public const string Property = "Property";
    public const string Force = "Force";
    public const string Confirm = "Confirm";
    public const string Save = "Save";
    public const string TurnOff = "TurnOff";
    public const string PassThru = "PassThru";
    public const string NewVHDPath = "NewVHDPath";
    public const string NewVHDSizeBytes = "NewVHDSizeBytes";
    public const string EnableSecureBoot = "EnableSecureBoot";
    public const string EnhancedSessionTransportType = "EnhancedSessionTransportType";
    public const string Generation = "Generation";
    public const string VHDPath = "VHDPath";
    public const string MemoryStartupBytes = "MemoryStartupBytes";
    public const string VM = "VM";
    public const string SwitchName = "SwitchName";
    public const string DefaultSwitchName = "Default Switch";
    public const string ParameterOnState = "On";
    public const string ParameterOffState = "Off";
    public const string ParameterHvSocket = "HvSocket";
    public const string ParameterVmBus = "VMBus";

    // Hyper-V psObject property values
    public const string CanStopService = "CanStop";
    public const string VMOffState = "Off";
    public const string RunningState = "Running";
    public const string PausedState = "Paused";
    public const string SavedState = "Saved";

    // Hyper-V scripts
    public const string VmConnectScript = "vmconnect.exe localhost -G";
}
