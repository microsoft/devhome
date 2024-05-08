// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;

namespace HyperVExtension.Services;

/// <summary>
/// The Stop-VM PowerShell cmdlet can either shutdown a VM by default via its OS, save
/// a VM's state or turn off a VM which is equivalent to disconnecting the power from
/// the virtual machine
/// </summary>
public enum StopVMKind
{
    Default,
    Save,
    TurnOff,
}

/// <summary> Class that handles interacting directly with Hyper-V.</summary>
public interface IHyperVManager
{
    /// <summary> Gets a boolean indicating whether the Hyper-V PowerShell module is available.</summary>
    public bool IsHyperVModuleLoaded();

    /// <summary> Starts the virtual machine management service if it is not running.</summary>
    public void StartVirtualMachineManagementService();

    /// <summary> Gets a list of Hyper-V virtual machines.</summary>
    /// <returns> A list of virtual machines.</returns>
    public IEnumerable<HyperVVirtualMachine> GetAllVirtualMachines();

    /// <summary> Gets a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    public HyperVVirtualMachine GetVirtualMachine(Guid vmId);

    /// <summary>Stops a Hyper-V virtual machine.</summary>
    /// <remarks>This can either Shuts down, turn off, or save a virtual machine's state.</remarks>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the virtual machine was stopped successfully, false otherwise.</returns>
    public bool StopVirtualMachine(Guid vmId, StopVMKind stopVMKind);

    /// <summary> Starts a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the virtual machine was started successfully, false otherwise.</returns>
    public bool StartVirtualMachine(Guid vmId);

    /// <summary> Pauses a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the virtual machine was paused successfully, false otherwise.</returns>
    public bool PauseVirtualMachine(Guid vmId);

    /// <summary> Resumes a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the virtual machine was resumed successfully, false otherwise.</returns>
    public bool ResumeVirtualMachine(Guid vmId);

    /// <summary> Removes a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the virtual machine was removed successfully, false otherwise.</returns>
    public bool RemoveVirtualMachine(Guid vmId);

    /// <summary> Gets a list of all checkpoints for a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> A list of checkpoints and their parent checkpoints </returns>
    public IEnumerable<Checkpoint> GetVirtualMachineCheckpoints(Guid vmId);

    /// <summary> Apply a Hyper-V virtual machines checkpoint.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <param name="checkpointId"> The id of the checkpoint for the virtual machine.</param>
    /// <returns> True if the checkpoint was applied successfully, false otherwise.</returns>
    public bool ApplyCheckpoint(Guid vmId, Guid checkpointId);

    /// <summary> Removes a Hyper-V virtual machines checkpoint.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <param name="checkpointId"> The id of the checkpoint for the virtual machine.</param>
    /// <returns> True if the checkpoint was removed successfully, false otherwise.</returns>
    public bool RemoveCheckpoint(Guid vmId, Guid checkpointId);

    /// <summary> Create a new checkpoint for a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the checkpoint was created successfully, false otherwise. </returns>
    public bool CreateCheckpoint(Guid vmId);

    /// <summary> Restarts a Hyper-V virtual machine.</summary>
    /// <param name="vmId"> The id of the virtual machine.</param>
    /// <returns> True if the virtual machine was started successfully, false otherwise.</returns>
    public bool RestartVirtualMachine(Guid vmId);

    /// <summary> Gets the disk size for a specific virtual disk</summary>
    /// <param name="diskPath"> The path to the virtual disk.</param>
    /// <returns> The size in bytes of the virtual disk.</returns>
    public ulong GetVhdSize(string diskPath);

    /// <summary> Gets the host information for the Hyper-V host.</summary>
    /// <returns> An object that represents the host information for the Hyper-V host.</returns>
    public HyperVVirtualMachineHost GetVirtualMachineHost();

    /// <summary> Creates a new virtual machine from the Hyper-V VM Gallery</summary>
    /// <param name="parameters"> The parameters for creating a new virtual machine.</param>
    /// <returns> A new virtual machine object.</returns>
    public HyperVVirtualMachine CreateVirtualMachineFromGallery(VirtualMachineCreationParameters parameters);
}
