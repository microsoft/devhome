// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Data;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Providers;
using HyperVExtension.Services;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HyperVExtension.Models;

/// <summary> Class that represents a Hyper-V virtual machine object. </summary>
public class HyperVVirtualMachine : IComputeSystem
{
    private readonly IHyperVManager _hyperVManager;

    private readonly PsObjectHelper _psObjectHelper;

    public event TypedEventHandler<IComputeSystem, ComputeSystemState> StateChanged = (s, e) => { };

    public Guid VmId => _psObjectHelper.MemberNameToValue<Guid>(HyperVStrings.Id);

    // IComputeSystem expects a string for the Id of the compute system.
    public string Id => VmId.ToString();

    public string? Name => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.Name);

    public int CPUUsage => _psObjectHelper.MemberNameToValue<int>(HyperVStrings.CPUUsage);

    public long MemoryAssigned => _psObjectHelper.MemberNameToValue<long>(HyperVStrings.MemoryAssigned);

    public long MemoryDemand => _psObjectHelper.MemberNameToValue<long>(HyperVStrings.MemoryDemand);

    public string? MemoryStatus => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.MemoryStatus);

    public TimeSpan Uptime => _psObjectHelper.MemberNameToValue<TimeSpan>(HyperVStrings.Uptime);

    public string? Status => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.Status);

    public string? State => _psObjectHelper.MemberNameToValue<Enum>(HyperVStrings.State)?.ToString();

    public bool DynamicMemoryEnabled => _psObjectHelper.MemberNameToValue<bool>(HyperVStrings.DynamicMemoryEnabled);

    public long MemoryMaximum => _psObjectHelper.MemberNameToValue<long>(HyperVStrings.MemoryMaximum);

    public long MemoryMinimum => _psObjectHelper.MemberNameToValue<long>(HyperVStrings.MemoryMinimum);

    public long MemoryStartup => _psObjectHelper.MemberNameToValue<long>(HyperVStrings.MemoryStartup);

    public long ProcessorCount => _psObjectHelper.MemberNameToValue<long>(HyperVStrings.ProcessorCount);

    public Guid ParentCheckpointId => _psObjectHelper.MemberNameToValue<Guid>(HyperVStrings.ParentCheckpointId);

    public string? ParentCheckpointName => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.ParentCheckpointName);

    public string? Path => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.Path);

    public DateTime CreationTime => _psObjectHelper.MemberNameToValue<DateTime>(HyperVStrings.CreationTime);

    public string? ComputerName => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.ComputerName);

    public bool IsDeleted => _psObjectHelper.MemberNameToValue<bool>(HyperVStrings.IsDeleted);

    // TODO: make getting this list dynamic so we can remove operations based on OS version.
    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Start |
                ComputeSystemOperations.ShutDown |
                ComputeSystemOperations.Terminate |
                ComputeSystemOperations.Delete |
                ComputeSystemOperations.Save |
                ComputeSystemOperations.Pause |
                ComputeSystemOperations.Resume |
                ComputeSystemOperations.CreateSnapshot |
                ComputeSystemOperations.DeleteSnapshot |
                ComputeSystemOperations.Restart |
                ComputeSystemOperations.ApplyConfiguration;

    public string AlternativeDisplayName { get; set; } = string.Empty;

    public IDeveloperId? AssociatedDeveloperId { get; set; }

    public string AssociatedProviderId { get; set; } = HyperVStrings.HyperVProviderId;

    public HyperVVirtualMachine(IHyperVManager hyperVManager, PSObject psObject)
    {
        _hyperVManager = hyperVManager;
        _psObjectHelper = new(psObject);
    }

    public IEnumerable<HyperVVirtualMachineHardDisk> GetHardDrives()
    {
        var returnList = new List<HyperVVirtualMachineHardDisk>();
        var hardDriveList = _psObjectHelper.MemberNameToValue<IEnumerable<object>>(HyperVStrings.HardDrives) ?? new List<object>();
        foreach (var hardDrive in hardDriveList)
        {
            returnList.Add(
                new HyperVVirtualMachineHardDisk
                {
                    ComputerName = _psObjectHelper.PropertyNameToValue<string>(hardDrive, HyperVStrings.ComputerName),
                    Name = _psObjectHelper.PropertyNameToValue<string>(hardDrive, HyperVStrings.Name),
                    Path = _psObjectHelper.PropertyNameToValue<string>(hardDrive, HyperVStrings.Path),
                    VmId = _psObjectHelper.PropertyNameToValue<Guid>(hardDrive, HyperVStrings.VmId),
                    VMName = _psObjectHelper.PropertyNameToValue<string>(hardDrive, HyperVStrings.VMName),
                    VMSnapshotId = _psObjectHelper.PropertyNameToValue<Guid>(hardDrive, HyperVStrings.VMSnapshotId),
                    VMSnapshotName = _psObjectHelper.PropertyNameToValue<string>(hardDrive, HyperVStrings.VMSnapshotName),
                });

            var disk = returnList.Last();
            if (disk.Path != null)
            {
                disk.DiskSizeInBytes = _hyperVManager.GetVhdSize(disk.Path);
            }
        }

        return returnList;
    }

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync(string options)
    {
        return Task.Run(() =>
        {
            var currentState = State switch
            {
                HyperVStrings.RunningState => ComputeSystemState.Running,
                HyperVStrings.VMOffState => ComputeSystemState.Stopped,
                HyperVStrings.PausedState => ComputeSystemState.Paused,
                HyperVStrings.SavedState => ComputeSystemState.Saved,
                _ => ComputeSystemState.Unknown,
            };

            return new ComputeSystemStateResult(currentState);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State == HyperVStrings.RunningState)
                {
                    // VM is already running.
                    return new ComputeSystemOperationResult();
                }

                StateChanged(this, ComputeSystemState.Starting);
                if (_hyperVManager.StartVirtualMachine(VmId))
                {
                    StateChanged(this, ComputeSystemState.Running);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Start));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Start);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Start), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State == HyperVStrings.VMOffState)
                {
                    // VM is already off.
                    return new ComputeSystemOperationResult();
                }

                StateChanged(this, ComputeSystemState.Stopping);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.Default))
                {
                    StateChanged(this, ComputeSystemState.Stopped);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.ShutDown));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.ShutDown);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.ShutDown));
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State == HyperVStrings.VMOffState)
                {
                    // VM is already off.
                    return new ComputeSystemOperationResult();
                }

                StateChanged(this, ComputeSystemState.Stopping);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.TurnOff))
                {
                    StateChanged(this, ComputeSystemState.Stopped);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Terminate));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Terminate);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Terminate), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Deleting);
                if (_hyperVManager.RemoveVirtualMachine(VmId))
                {
                    StateChanged(this, ComputeSystemState.Deleted);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Delete));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Delete);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Delete), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State == HyperVStrings.SavedState)
                {
                    // VM is already saved.
                    return new ComputeSystemOperationResult();
                }

                StateChanged(this, ComputeSystemState.Saving);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.Save))
                {
                    StateChanged(this, ComputeSystemState.Saved);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Save));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Save);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Save), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State == HyperVStrings.PausedState)
                {
                    // VM is already paused.
                    return new ComputeSystemOperationResult();
                }

                StateChanged(this, ComputeSystemState.Pausing);
                if (_hyperVManager.PauseVirtualMachine(VmId))
                {
                    StateChanged(this, ComputeSystemState.Paused);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Pause));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Pause);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Pause), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State == HyperVStrings.RunningState)
                {
                    // VM is already running.
                    return new ComputeSystemOperationResult();
                }

                StateChanged(this, ComputeSystemState.Starting);
                if (_hyperVManager.ResumeVirtualMachine(VmId))
                {
                    StateChanged(this, ComputeSystemState.Running);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Resume));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Resume);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Resume), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_hyperVManager.CreateCheckpoint(VmId))
                {
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.CreateSnapshot));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.CreateSnapshot);
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.CreateSnapshot), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // Reverting checkpoints means applying the previous checkpoint onto the VM.
                if (_hyperVManager.ApplyCheckpoint(VmId, ParentCheckpointId))
                {
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.RevertSnapshot));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.RevertSnapshot);
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.RevertSnapshot), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // For v1 we only support deleting the previous checkpoint.
                if (_hyperVManager.RemoveCheckpoint(VmId, ParentCheckpointId))
                {
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.DeleteSnapshot));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.DeleteSnapshot);
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.DeleteSnapshot), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                _hyperVManager.ConnectToVirtualMachine(VmId);
                Logging.Logger()?.ReportInfo($"Successful vmconnect launch attempt on {DateTime.Now}: VM details: {this}");
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError($"Failed to launch vmconnect on {DateTime.Now}: VM details: {this}", ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                if (State != HyperVStrings.RunningState)
                {
                    throw new ComputeSystemOperationException(ComputeSystemOperations.Restart);
                }

                StateChanged(this, ComputeSystemState.Restarting);
                if (_hyperVManager.RestartVirtualMachine(VmId))
                {
                    StateChanged(this, ComputeSystemState.Running);
                    Logging.Logger()?.ReportInfo(OperationSuccessString(ComputeSystemOperations.Restart));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Restart);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                Logging.Logger()?.ReportError(OperationErrorString(ComputeSystemOperations.Restart), ex);
                return new ComputeSystemOperationResult(ex, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        return Task.Run(async () =>
        {
            var uri = new Uri(Constants.WindowsThumbnail);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var randomAccessStream = await storageFile.OpenReadAsync();

            // Convert the stream to a byte array
            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);
            return new ComputeSystemThumbnailResult(bytes);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // For hyper-v we'll provide storage as the total allocated size of all virtual hard disks
                // assigned to the virtual machine.
                var totalDiskSize = 0ul;
                foreach (var disk in GetHardDrives())
                {
                    totalDiskSize += disk.DiskSizeInBytes;
                }

                // Only specific properties are supported for now.
                var properties = new List<ComputeSystemProperty>
                {
                    new(ProcessorCount, ComputeSystemPropertyKind.CpuCount),
                    new(MemoryAssigned, ComputeSystemPropertyKind.AssignedMemorySizeInBytes),
                    new(totalDiskSize, ComputeSystemPropertyKind.StorageSizeInBytes),
                    new(Uptime, ComputeSystemPropertyKind.UptimeIn100ns),

                    // TODO: localize this property name.
                    new("Current Checkpoint", ParentCheckpointName, ComputeSystemPropertyKind.Generic),
                };

                return properties.AsEnumerable();
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError($"Failed to GetComputeSystemPropertiesAsync on {DateTime.Now}: VM details: {this}", ex);
                return new List<ComputeSystemProperty>();
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string options)
    {
        // This is temporary until we have a proper implementation for this.
        var notImplementedException = new NotImplementedException($"Method not implemented by Hyper-V Compute Systems: VM details: {this}");
        return Task.FromResult(new ComputeSystemOperationResult(notImplementedException, notImplementedException.Message)).AsAsyncOperation();
    }

    public IApplyConfigurationOperation? ApplyConfiguration(string configuration)
    {
        // This is temporary until we have a proper implementation for this.
        Logging.Logger()?.ReportError($"Configuration not supported yet for hyper-v");
        return null;
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM Id: {Id} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM Name: {Name} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM CreationTime: {CreationTime} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM State: {State} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM Status: {Status} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM CPUUsage: {CPUUsage}% ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM Uptime: {Uptime} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM MemoryStatus: {MemoryStatus} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM MemoryAssigned: {BytesHelper.ConvertFromBytes((ulong)MemoryAssigned)} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM MemoryDemand: {BytesHelper.ConvertFromBytes((ulong)MemoryDemand)} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM StartupMemory: {BytesHelper.ConvertFromBytes((ulong)MemoryStartup)} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM MinimumMemory: {BytesHelper.ConvertFromBytes((ulong)MemoryMinimum)} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM MaximumMemory: {BytesHelper.ConvertFromBytes((ulong)MemoryMaximum)} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM DynamicMemoryEnabled: {DynamicMemoryEnabled} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM ParentCheckpointId: {ParentCheckpointId} ");

        var hardDisks = GetHardDrives();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Number of Harddisks: {hardDisks.Count()} ");

        foreach (var hardDisk in hardDisks)
        {
            builder.AppendLine(hardDisk.ToString());
        }

        return builder.ToString();
    }

    private string OperationErrorString(ComputeSystemOperations operation)
    {
        return $"Failed to complete {operation} operation on {DateTime.Now}: VM details: {this}";
    }

    private string OperationSuccessString(ComputeSystemOperations operation)
    {
        return $"Successfully completed {operation} operation on {DateTime.Now}: VM details: {this}";
    }
}
