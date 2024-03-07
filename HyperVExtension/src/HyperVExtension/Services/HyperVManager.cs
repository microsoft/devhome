// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;
using System.Security.Principal;
using System.ServiceProcess;
using DevHome.Logging;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Models;
using HyperVExtension.Providers;
using Microsoft.Extensions.Hosting;

namespace HyperVExtension.Services;

/// <summary>
/// Class that interacts with the Hyper-V service and is used for all Hyper-V related
/// functionality.
/// </summary>
public class HyperVManager : IHyperVManager, IDisposable
{
    private readonly IPowerShellService _powerShellService;

    private readonly HyperVVirtualMachineFactory _hyperVVirtualMachineFactory;

    private readonly IHost _host;

    private readonly object _operationLock = new();

    public bool IsFirstTimeLoadingModule { get; private set; } = true;

    /// <summary>
    /// This dictionary is used so we can map a virtual machines id to the amount of operations
    /// that were requested of us to perform for it. We should only perform one operation per virtual machine
    /// at a time. The manager is still able perform multiple operations at time assuming each operation is
    /// for a different virtual machine.
    /// </summary>
    private readonly Dictionary<Guid, uint> _virtualMachinesToOperateOn = new();

    private readonly AutoResetEvent _operationEventForVirtualMachine = new(false);

    private const uint _numberOfOperationsToPeformPerVirtualMachine = 1;

    private readonly TimeSpan _serviceTimeoutInSeconds = TimeSpan.FromSeconds(3);

    private bool _disposed;

    public HyperVManager(IHost host, IPowerShellService powerShellService, HyperVVirtualMachineFactory hyperVVirtualMachineFactory)
    {
        _powerShellService = powerShellService;
        _host = host;
        _hyperVVirtualMachineFactory = hyperVVirtualMachineFactory;
    }

    /// <inheritdoc cref="IHyperVManager.IsHyperVModuleLoaded"/>
    public bool IsHyperVModuleLoaded()
    {
        if (IsFirstTimeLoadingModule)
        {
            IsFirstTimeLoadingModule = false;
            LoadHyperVModule();
        }

        // Build command line statement to get all the available modules.
        // Work around for .Net 8 and PowerShell.SDK 7.4.* issue where the PowerShell session
        // Can't find the module, even though it appears in a regular PowerShell terminal window.
        // this will be removed once the issue is resolved.
        var commandLineStatements = new StatementBuilder()
            .AddScript("Get-Module -ListAvailable", true)
            .Build();

        var result = _powerShellService.Execute(commandLineStatements, PipeType.None);
        var moduleFound = result.PsObjects?.Any(psObject =>
        {
            var helper = new PsObjectHelper(psObject);
            return helper.MemberNameToValue<string>(HyperVStrings.Name) == HyperVStrings.HyperVModuleName;
        }) ?? false;

        if (!moduleFound)
        {
            Logging.Logger()?.ReportWarn($"PowerShell could not find the Hyper-V module in the list of modules loaded into the current session: {result.CommandOutputErrorMessage}");
        }

        return moduleFound;
    }

    private void LoadHyperVModule()
    {
        // Makes sure the Hyper-V module is loaded in the current PowerShell session.
        // After moving to .Net 8 and using PowerShell.SDK 7.4.*, simply attempting to
        // import the Hyper-V module from Dev Home does not work. We need to force the
        // module by attempting to load it twice.
        // A work around is to use the Get-Module twice in the PowerShell session
        // to find the Hyper-V module. I'll need to investigate this further.
        var commandLineStatements = new StatementBuilder()
            .AddCommand(HyperVStrings.GetModule)
            .AddParameter(HyperVStrings.ListAvailable, true)
            .AddParameter(HyperVStrings.Name, HyperVStrings.HyperVModuleName)
            .Build();

        var result = _powerShellService.Execute(commandLineStatements, PipeType.None);

        if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
        {
            Logging.Logger()?.ReportWarn($"PowerShell returned an error while attempting to get the Hyper-V module on the first try: {result.CommandOutputErrorMessage}");
        }
    }

    /// <inheritdoc cref="IHyperVManager.StartVirtualMachineManagementService"/>
    public void StartVirtualMachineManagementService()
    {
        if (!IsUserInHyperVAdminGroup())
        {
            throw new HyperVAdminGroupException("The current logged on user is not in the Hyper-V administrator group");
        }

        if (!IsHyperVModuleLoaded())
        {
            // we won't throw an exception here. If there is a cmdlet failure due to the module not being loaded, we'll let the
            // PowerShell cmdlet throw the exception.
            Logging.Logger()?.ReportError("The Hyper-V PowerShell Module is not Loaded");
        }

        var serviceController = _host.GetService<IWindowsServiceController>();
        serviceController.ServiceName = HyperVStrings.VMManagementService;

        switch (serviceController.Status)
        {
            case ServiceControllerStatus.Running:
                // The service is already running
                return;
            case ServiceControllerStatus.StartPending:
                // The service is starting, so we'll wait to confirm it started.
                break;

            // If the service is stopping, we'll wait till its fully stopped.
            case ServiceControllerStatus.StopPending:
                serviceController.WaitForStatusChange(ServiceControllerStatus.Stopped, _serviceTimeoutInSeconds);
                goto case ServiceControllerStatus.Stopped;

            case ServiceControllerStatus.Stopped:
                // Service is stopped try to start it.
                serviceController.StartService();
                break;

            // If the service is pausing, we'll wait till its fully paused.
            case ServiceControllerStatus.PausePending:
                serviceController.WaitForStatusChange(ServiceControllerStatus.Paused, _serviceTimeoutInSeconds);
                goto case ServiceControllerStatus.Paused;

            case ServiceControllerStatus.Paused:
                // If the service is paused, try to resume it.
                serviceController.ContinueService();
                break;
        }

        // wait for service to start.
        serviceController.WaitForStatusChange(ServiceControllerStatus.Running, _serviceTimeoutInSeconds);
    }

    /// <inheritdoc cref="IHyperVManager.GetAllVirtualMachines"/>
    public IEnumerable<HyperVVirtualMachine> GetAllVirtualMachines()
    {
        StartVirtualMachineManagementService();

        // Build command line statement to get all the the VMs on the machine.
        var commandLineStatements = new StatementBuilder().AddCommand(HyperVStrings.GetVM).Build();
        var result = _powerShellService.Execute(commandLineStatements, PipeType.None);

        if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
        {
            // Note: errors here could be about retrieving 1 out of N virtual machines, so we log this and return the rest.
            Logging.Logger()?
                .ReportWarn($"Unable to get all VMs due to PowerShell error: {result.CommandOutputErrorMessage}");
        }

        var returnList = result.PsObjects?
            .Where(psObject => psObject != null)
            .Select(psObject => _hyperVVirtualMachineFactory(psObject));

        return returnList ?? new List<HyperVVirtualMachine>();
    }

    /// <inheritdoc cref="IHyperVManager.GetVirtualMachine"/>
    public HyperVVirtualMachine GetVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();
        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(GetVMCommandLineStatement(vmId), PipeType.None);

            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to get VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // If we found the VM there should only be one psObject in the list.
            var psObject = result.PsObjects?.FirstOrDefault();
            if (psObject != null)
            {
                return _hyperVVirtualMachineFactory(psObject);
            }

            throw new HyperVManagerException($"Unable to get VM with Id {vmId} due to PowerShell returning a null PsObject");
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.StopVirtualMachine"/>
    public bool StopVirtualMachine(Guid vmId, StopVMKind stopVMKind)
    {
        StartVirtualMachineManagementService();

        // Start building default command line statement to stop the VM.
        var statementBuilder = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.StopVM)
            .AddParameter(HyperVStrings.PassThru, true);

        var endStateString = HyperVStrings.VMOffState;

        if (stopVMKind == StopVMKind.Save)
        {
            // Add parameter to change the Stop-VM cmdlets behavior to save the VM's state instead of shutting it down.
            statementBuilder.AddParameter(HyperVStrings.Save, true);
            endStateString = HyperVStrings.SavedState;
        }
        else if (stopVMKind == StopVMKind.TurnOff)
        {
            // Add parameter to change the Stop-VM cmdlets behavior to turn off the VM immediately instead of shutting it down.
            statementBuilder.AddParameter(HyperVStrings.TurnOff, true);
        }

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(statementBuilder.Build(), PipeType.PipeOutput);

            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to stop VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // The VM will be the returned object since we used the "PassThru" parameter.
            var vmObject = result.PsObjects.FirstOrDefault();
            if (vmObject == null)
            {
                return false;
            }

            var virtualMachine = _hyperVVirtualMachineFactory(vmObject);

            // If the current state and endstate are the same we were able to stop the VM successfully.
            return AreStringsTheSame(virtualMachine?.State, endStateString);
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.StartVirtualMachine"/>
    public bool StartVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();

        // Start building command line statement to start the VM.
        var statementBuilder = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.StartVM)
            .AddParameter(HyperVStrings.PassThru, true);

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(statementBuilder.Build(), PipeType.PipeOutput);

            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to start VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // The VM will be the returned object since we used the "PassThru" parameter.
            var vmObject = result.PsObjects.FirstOrDefault();
            if (vmObject == null)
            {
                return false;
            }

            var virtualMachine = _hyperVVirtualMachineFactory(vmObject);

            // Check if we were able to turn on the VM successfully.
            return AreStringsTheSame(virtualMachine?.State, HyperVStrings.RunningState);
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.PauseVirtualMachine"/>
    public bool PauseVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();

        // Start building command line statement to pause the VM.
        var statementBuilder = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.SuspendVM)
            .AddParameter(HyperVStrings.PassThru, true);

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(statementBuilder.Build(), PipeType.PipeOutput);

            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to pause VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // The VM will be the returned object since we used the "PassThru" parameter.
            var vmObject = result.PsObjects.FirstOrDefault();
            if (vmObject == null)
            {
                return false;
            }

            var virtualMachine = _hyperVVirtualMachineFactory(vmObject);

            // Check if we were able to pause the VM successfully.
            return AreStringsTheSame(virtualMachine?.State, HyperVStrings.PausedState);
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.ResumeVirtualMachine"/>
    public bool ResumeVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();
        var statementBuilder = new StatementBuilder();

        // Build command line statement to resume the VM.
        var commandLineStatements = statementBuilder
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.ResumeVM)
            .AddParameter(HyperVStrings.PassThru, true)
            .Build();

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);

            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to resume VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // The VM will be the returned object since we used the "PassThru" parameter.
            var vmObject = result.PsObjects.FirstOrDefault();
            if (vmObject == null)
            {
                return false;
            }

            var virtualMachine = _hyperVVirtualMachineFactory(vmObject);

            // Check if we were able to resume the VM successfully.
            return AreStringsTheSame(virtualMachine?.State, HyperVStrings.RunningState);
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.RemoveVirtualMachine"/>
    public bool RemoveVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();
        var statementBuilder = new StatementBuilder();

        // Build command line statement to remove the VM.
        var commandLineStatements = statementBuilder
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.RemoveVM)
            .AddParameter(HyperVStrings.Force, true)
            .AddParameter(HyperVStrings.PassThru, true)
            .Build();

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);
            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to remove VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // The VM will be the returned object since we used the "PassThru" parameter.
            var vmObject = result.PsObjects.FirstOrDefault();
            if (vmObject == null)
            {
                return false;
            }

            var virtualMachine = _hyperVVirtualMachineFactory(vmObject);
            return virtualMachine.IsDeleted;
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.ConnectToVirtualMachine"/>
    public void ConnectToVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();
        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            // Build command line statement to connect to the VM.
            var commandLineStatements = new StatementBuilder()
                .AddScript($"{HyperVStrings.VmConnectScript} {vmId}", true)
                .Build();

            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);

            // Note: We use the vmconnect application to connect to the VM. VM connect will display a message box with
            // an error if one occurs. We will only throw this error if an error occurs within the PowerShell session.
            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to launch VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.GetVirtualMachineCheckpoints"/>
    public IEnumerable<Checkpoint> GetVirtualMachineCheckpoints(Guid vmId)
    {
        StartVirtualMachineManagementService();

        // Build command line statement to get all the checkpoints.
        var commandLineStatements = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.GetVMSnapshot)
            .Build();

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);
            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                // Note: errors here could be about retrieving 1 out of N checkpoints, so we log this and return the rest.
                Logging.Logger()?
                    .ReportWarn($"Unable to get all checkpoints for VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            var checkpointList = result.PsObjects?.Select(psObject =>
            {
                var helper = new PsObjectHelper(psObject);
                var checkpointId = helper.MemberNameToValue<Guid>(HyperVStrings.Id);
                var checkpointName = helper.MemberNameToValue<string>(HyperVStrings.Name) ?? string.Empty;
                var parentCheckpointId = helper.MemberNameToValue<Guid>(HyperVStrings.ParentCheckpointId);
                var parentCheckpointName = helper.MemberNameToValue<string>(HyperVStrings.ParentCheckpointName) ?? string.Empty;
                return new Checkpoint(parentCheckpointId, parentCheckpointName, checkpointId, checkpointName);
            });

            return checkpointList ?? new List<Checkpoint>();
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.ApplyCheckpoint"/>
    public bool ApplyCheckpoint(Guid vmId, Guid checkpointId)
    {
        StartVirtualMachineManagementService();

        // Build command line statement to apply the checkpoint.
        var commandLineStatements = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVMSnapshot)
            .AddParameter(HyperVStrings.Id, checkpointId)
            .AddCommand(HyperVStrings.RestoreVMSnapshot)
            .AddParameter(HyperVStrings.Confirm, false)
            .Build();

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);
            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException(
                    $"Unable to apply the checkpoint with Id: {checkpointId} for VM with Id: {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // Build command line statement to get the VM so we can confirm that the checkpoint was applied.
            var virtualMachine = ExecuteAndReturnObject<HyperVVirtualMachine>(GetVMCommandLineStatement(vmId), PipeType.None);

            if (virtualMachine == null)
            {
                return false;
            }

            // If the parentCheckpointId of the current VM is equal to the one that was passed in, we applied it successfully.
            return virtualMachine.ParentCheckpointId == checkpointId;
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.RemoveCheckpoint"/>
    public bool RemoveCheckpoint(Guid vmId, Guid checkpointId)
    {
        StartVirtualMachineManagementService();
        var statementBuilder = new StatementBuilder();

        // Build command line statement to remove the checkpoint.
        var commandLineStatements = statementBuilder
            .AddCommand(HyperVStrings.GetVMSnapshot)
            .AddParameter(HyperVStrings.Id, checkpointId)
            .AddCommand(HyperVStrings.RemoveVMSnapshot)
            .AddParameter(HyperVStrings.Confirm, false)
            .Build();

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);
            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException(
                    $"Unable to remove the checkpoint with Id: {checkpointId} for VM with Id: {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // Build command line statement to get the VM's checkpoints so we can confirm that the checkpoint was removed.
            commandLineStatements = statementBuilder
                .AddCommand(HyperVStrings.GetVM)
                .AddParameter(HyperVStrings.Id, vmId)
                .AddCommand(HyperVStrings.GetVMSnapshot)
                .Build();

            result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);

            if (result.PsObjects != null)
            {
                // Check if the checkpoint still exists. At this point it should not exist if we removed it successfully.
                var wasCheckPointRemoved = !result.PsObjects.Any(psObject =>
                    new PsObjectHelper(psObject).MemberNameToValue<Guid>(HyperVStrings.Id) == checkpointId);

                return wasCheckPointRemoved;
            }

            return false;
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.CreateCheckpoint"/>
    public bool CreateCheckpoint(Guid vmId)
    {
        StartVirtualMachineManagementService();

        // Build command line statement to create the new checkpoint and then return it as a PowerShell object.
        var commandLineStatements = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.CreateVMCheckpoint)
            .AddParameter(HyperVStrings.PassThru, true)
            .Build();

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);
            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException(
                    $"Unable to create a new checkpoint for VM with Id: {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            var newCheckpoint = result.PsObjects.FirstOrDefault();
            if (newCheckpoint != null)
            {
                // Get the checkpoint id of the returned checkpoint.
                var helper = new PsObjectHelper(newCheckpoint);
                var newCheckpointId = helper.MemberNameToValue<Guid>(HyperVStrings.Id);

                // The id of the new checkpoint should be a non empty Guid.
                return newCheckpointId != Guid.Empty;
            }

            return false;
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.RestartVirtualMachine"/>
    public bool RestartVirtualMachine(Guid vmId)
    {
        StartVirtualMachineManagementService();

        // Start building command line statement to start the VM.
        var statementBuilder = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVM)
            .AddParameter(HyperVStrings.Id, vmId)
            .AddCommand(HyperVStrings.RestartVM)
            .AddParameter(HyperVStrings.Force, true)
            .AddParameter(HyperVStrings.PassThru, true);

        AddVirtualMachineToOperationsMap(vmId);

        try
        {
            var result = _powerShellService.Execute(statementBuilder.Build(), PipeType.PipeOutput);

            if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
            {
                throw new HyperVManagerException($"Unable to start VM with Id {vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
            }

            // The VM will be the returned object since we used the "PassThru" parameter.
            var vmObject = result.PsObjects.FirstOrDefault();
            if (vmObject == null)
            {
                return false;
            }

            // confirm the VM was started successfully.
            var virtualMachine = _hyperVVirtualMachineFactory(vmObject);
            return virtualMachine.State == HyperVStrings.RunningState;
        }
        finally
        {
            RemoveVirtualMachineFromOperationsMap(vmId);
        }
    }

    /// <inheritdoc cref="IHyperVManager.GetVhdSize"/>
    public ulong GetVhdSize(string diskPath)
    {
        var statementBuilder = new StatementBuilder()
            .AddCommand(HyperVStrings.GetVHD)
            .AddParameter(HyperVStrings.Path, diskPath);

        var result = _powerShellService.Execute(statementBuilder.Build(), PipeType.PipeOutput);

        if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
        {
            throw new HyperVManagerException($"Unable to get disk size due to PowerShell error: {result.CommandOutputErrorMessage}");
        }

        // object in the returned results should represent a virtual disk.
        var vmObject = result.PsObjects.FirstOrDefault();
        if (vmObject == null)
        {
            // If the VM object is null we were unable to get the disk size.
            return 0;
        }

        var helper = new PsObjectHelper(vmObject);
        return helper.MemberNameToValue<ulong>(HyperVStrings.Size);
    }

    /// <inheritdoc cref="IHyperVManager.IsUserInHyperVAdminGroup"/>
    public bool IsUserInHyperVAdminGroup()
    {
        var currentUser = _host.GetService<IWindowsIdentityService>().GetCurrentWindowsIdentity();
        var wasHyperVSidFound = currentUser?.Groups?.Any(sid => sid.Value == HyperVStrings.HyperVAdminGroupWellKnownSid);
        return wasHyperVSidFound ?? false;
    }

    private bool AreStringsTheSame(string? stringA, string? stringB)
    {
        return stringA?.Equals(stringB, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Helper method that executes a list of PowerShell commandline statements
    /// and returns the given T object or its default value.
    /// </summary>
    private T? ExecuteAndReturnObject<T>(List<PowerShellCommandlineStatement> statements, PipeType pipeType)
    {
        var result = _powerShellService.Execute(statements, pipeType);
        var psObject = result.PsObjects.FirstOrDefault();
        if (psObject == null)
        {
            Logging.Logger()?.ReportError($"Unable to create {nameof(T)} due to PowerShell error: {result.CommandOutputErrorMessage}");
            return default(T);
        }

        if (typeof(T) == typeof(HyperVVirtualMachine) && _hyperVVirtualMachineFactory(psObject) is T virtualMachine)
        {
            return virtualMachine;
        }

        if (typeof(T) == typeof(PsObjectHelper) && new PsObjectHelper(psObject) is T psObjecthelper)
        {
            return psObjecthelper;
        }

        return default(T);
    }

    /// <summary> Helper method that is used to get the PowerShell commandline statement for retrieving a specific virtual machine object.</summary>
    private List<PowerShellCommandlineStatement> GetVMCommandLineStatement(Guid vmId)
    {
        // Build command line statement to get a specific VM.
        return new StatementBuilder().AddCommand(HyperVStrings.GetVM).AddParameter(HyperVStrings.Id, vmId).Build();
    }

    /// <summary>
    /// Adds the Id of the virtual machine to the _virtualMachinesToOperateOn dictionary. This makes sure
    /// we perform only one operation per virtual machine. We do this by incrementing the value belonging
    /// to the key (vm guid), when a request comes in to perform an operation on the VM. When the number
    /// of operations queued up for the VM exceeds _numberOfOperationsToPeformPerVirtualMachine the thread
    /// will wait until it is signalled to proceed.
    /// </summary>
    private void AddVirtualMachineToOperationsMap(Guid vmId)
    {
        var managerCurrentlyDoingOperationOnVM = false;

        lock (_operationLock)
        {
            // increment the number of operations being performed by the manager on the VM by 1
            // each time we enter the lock.
            _virtualMachinesToOperateOn.TryGetValue(vmId, out var queuedOperationsForThisVm);
            _virtualMachinesToOperateOn[vmId] = queuedOperationsForThisVm + 1;
            if (_virtualMachinesToOperateOn[vmId] > _numberOfOperationsToPeformPerVirtualMachine)
            {
                managerCurrentlyDoingOperationOnVM = true;
            }
        }

        if (managerCurrentlyDoingOperationOnVM)
        {
            // Wait to be signalled when the previous operation on the VM has completed.
            _operationEventForVirtualMachine.WaitOne();
        }
    }

    /// <summary>
    /// decrements the operation queue count and signals to a waiting thread that it can now proceed out
    /// of the waiting state.
    /// </summary>
    private void RemoveVirtualMachineFromOperationsMap(Guid vmId)
    {
        lock (_operationLock)
        {
            // Decrement the number of queued operations by one now that the operation has completed.
            _virtualMachinesToOperateOn.TryGetValue(vmId, out var queuedOperationsForThisVm);
            _virtualMachinesToOperateOn[vmId] = queuedOperationsForThisVm - 1;

            // Set the signal to allow waiting threads to proceed
            _operationEventForVirtualMachine.Set();
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            LogEvent.Create(
                nameof(HyperVManager),
                string.Empty,
                SeverityLevel.Debug,
                "Disposing HyperVManager");

            if (disposing)
            {
                _operationEventForVirtualMachine.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
