// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using HyperVExtension.Common;
using HyperVExtension.Common.Extensions;
using HyperVExtension.CommunicationWithGuest;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.HostGuestCommunication;
using HyperVExtension.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.Models;

public delegate HyperVVirtualMachine HyperVVirtualMachineFactory(PSObject pSObject);

/// <summary> Class that represents a Hyper-V virtual machine object. </summary>
public partial class HyperVVirtualMachine : IComputeSystem
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(HyperVVirtualMachine));

    private readonly string _errorResourceKey = "ErrorPerformingOperation";

    private readonly string _currentCheckpointKey = "CurrentCheckpoint";

    private readonly IStringResource _stringResource;

    private readonly IHost _host;
    private readonly IHyperVManager _hyperVManager;

    private readonly PsObjectHelper _psObjectHelper;

    public event TypedEventHandler<IComputeSystem, ComputeSystemState> StateChanged = (s, e) => { };

    public Guid VmId => _psObjectHelper.MemberNameToValue<Guid>(HyperVStrings.Id);

    // IComputeSystem expects a string for the Id of the compute system.
    public string Id => VmId.ToString();

    public string? DisplayName => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.Name);

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

    private bool _isDeleted;

    public string OperationErrorUnknownString => _stringResource.GetLocalized(_errorResourceKey, Logging.LogFolderRoot);

    public ComputeSystemOperations SupportedOperations
    {
        get
        {
            if (_isDeleted)
            {
                return ComputeSystemOperations.None;
            }

            // Before applying the configuration we start the VM. So we will allow the ApplyConfiguration to be the base supported operation
            // for the VM.
            var supportedOperations = ComputeSystemOperations.ApplyConfiguration;
            var revertOperation = Guid.Empty.Equals(ParentCheckpointId) ? ComputeSystemOperations.None : ComputeSystemOperations.RevertSnapshot;

            switch (GetState())
            {
                case ComputeSystemState.Running:
                    // Supported operations when running
                    supportedOperations = ComputeSystemOperations.ShutDown |
                        ComputeSystemOperations.Terminate |
                        ComputeSystemOperations.Save |
                        ComputeSystemOperations.Pause |
                        ComputeSystemOperations.CreateSnapshot |
                        ComputeSystemOperations.Restart |
                        revertOperation;
                    break;
                case ComputeSystemState.Stopped:
                    // Supported operations when stopped
                    supportedOperations = ComputeSystemOperations.Start |
                        ComputeSystemOperations.CreateSnapshot |
                        ComputeSystemOperations.Delete;
                    break;
                case ComputeSystemState.Saved:
                    // Supported operations when saved
                    supportedOperations = ComputeSystemOperations.Start |
                        ComputeSystemOperations.CreateSnapshot |
                        ComputeSystemOperations.Delete |
                        revertOperation;
                    break;
                case ComputeSystemState.Paused:
                    // Supported operations when paused
                    supportedOperations = ComputeSystemOperations.Terminate |
                        ComputeSystemOperations.Save |
                        ComputeSystemOperations.Resume |
                        ComputeSystemOperations.CreateSnapshot |
                        revertOperation;
                    break;
            }

            // Disable ApplyConfiguration for ARM
            var arch = RuntimeInformation.OSArchitecture;
            if (arch == Architecture.Arm64 || arch == Architecture.Arm || arch == Architecture.Armv6)
            {
                return supportedOperations;
            }

            return supportedOperations | ComputeSystemOperations.ApplyConfiguration;
        }
    }

    public string SupplementalDisplayName { get; set; } = string.Empty;

    public IDeveloperId? AssociatedDeveloperId { get; set; }

    public string AssociatedProviderId { get; set; } = HyperVStrings.HyperVProviderId;

    public HyperVVirtualMachine(IHost host, IHyperVManager hyperVManager, IStringResource stringResource, PSObject psObject)
    {
        _host = host;
        _hyperVManager = hyperVManager;
        _psObjectHelper = new(psObject);
        _stringResource = stringResource;
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

    private ComputeSystemState GetState()
    {
        return State switch
        {
            HyperVStrings.RunningState => ComputeSystemState.Running,
            HyperVStrings.VMOffState => ComputeSystemState.Stopped,
            HyperVStrings.PausedState => ComputeSystemState.Paused,
            HyperVStrings.SavedState => ComputeSystemState.Saved,
            HyperVStrings.SavingState => ComputeSystemState.Saving,
            HyperVStrings.PausingState => ComputeSystemState.Pausing,
            HyperVStrings.StartingState => ComputeSystemState.Starting,
            HyperVStrings.ResumingState => ComputeSystemState.Starting,
            HyperVStrings.StoppingState => ComputeSystemState.Stopping,
            _ => ComputeSystemState.Unknown,
        };
    }

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task.Run(() =>
        {
            return new ComputeSystemStateResult(GetState());
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options)
    {
        return Task.Run(() =>
        {
            return Start(options);
        }).AsAsyncOperation();
    }

    private ComputeSystemOperationResult Start(string options)
    {
        try
        {
            StateChanged(this, ComputeSystemState.Starting);
            if (_hyperVManager.StartVirtualMachine(VmId))
            {
                StateChanged(this, GetState());
                _log.Information(OperationSuccessString(ComputeSystemOperations.Start));
                return new ComputeSystemOperationResult();
            }

            throw new ComputeSystemOperationException(ComputeSystemOperations.Start);
        }
        catch (Exception ex)
        {
            StateChanged(this, GetState());
            _log.Error(ex, OperationErrorString(ComputeSystemOperations.Start));
            return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
        }
    }

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Stopping);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.Default))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.ShutDown));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.ShutDown);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(OperationErrorString(ComputeSystemOperations.ShutDown));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Stopping);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.TurnOff))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.Terminate));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Terminate);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.Terminate));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
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
                    _isDeleted = true;
                    StateChanged(this, ComputeSystemState.Deleted);
                    _log.Information(OperationSuccessString(ComputeSystemOperations.Delete));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Delete);
            }
            catch (Exception ex)
            {
                StateChanged(this, ComputeSystemState.Unknown);
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.Delete));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Saving);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.Save))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.Save));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Save);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.Save));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Pausing);
                if (_hyperVManager.PauseVirtualMachine(VmId))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.Pause));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Pause);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.Pause));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Starting);
                if (_hyperVManager.ResumeVirtualMachine(VmId))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.Resume));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Resume);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.Resume));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // we don't currently have a state for creating a checkpoint so we'll say we're in the saving state
                StateChanged(this, ComputeSystemState.Saving);
                if (_hyperVManager.CreateCheckpoint(VmId))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.CreateSnapshot));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.CreateSnapshot);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.CreateSnapshot));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // we don't currently have a state for reverting a checkpoint so we'll say we're in the saving state
                StateChanged(this, ComputeSystemState.Saving);

                // Reverting checkpoints means applying the previous checkpoint onto the VM.
                if (_hyperVManager.ApplyCheckpoint(VmId, ParentCheckpointId))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.RevertSnapshot));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.RevertSnapshot);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.RevertSnapshot));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // we don't currently have a state for deleting a checkpoint so we'll say we're in the saving state
                StateChanged(this, ComputeSystemState.Saving);

                // For v1 we only support deleting the previous checkpoint.
                if (_hyperVManager.RemoveCheckpoint(VmId, ParentCheckpointId))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.DeleteSnapshot));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.DeleteSnapshot);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.DeleteSnapshot));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                _log.Information($"Starting vmconnect launch attempt: VM details: {this}");
                ProcessStartInfo processStartInfo = new ProcessStartInfo("vmconnect.exe");
                processStartInfo.UseShellExecute = true;

                // The -G flag allows us to use the Id of the VM to tell vmconnect.exe which VM to launch into.
                processStartInfo.Arguments = $"localhost -G {Id}";

                using (Process vmConnectProcess = new Process())
                {
                    vmConnectProcess.StartInfo = processStartInfo;

                    // We start the process and will return success if it does not throw an exception.
                    // If vmconnect has an error, it will be displayed to the user in a message dialog
                    // outside of our control.
                    vmConnectProcess.Start();

                    // Note: Just because the vmconnect.exe launches in the foreground does not mean it will launch
                    // in front of the Dev Home window. Since the vmconnect.exe is a separate process being launched
                    // outside of Dev Home, it will not be parented to the Dev Home window. The shell will launch it
                    // in its last known location.
                    PInvoke.SetForegroundWindow((HWND)vmConnectProcess.MainWindowHandle);
                }

                _log.Information($"Successful vmconnect launch attempt: VM details: {this}");
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to launch vmconnect: VM details: {this}");
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged(this, ComputeSystemState.Restarting);
                if (_hyperVManager.RestartVirtualMachine(VmId))
                {
                    StateChanged(this, GetState());
                    _log.Information(OperationSuccessString(ComputeSystemOperations.Restart));
                    return new ComputeSystemOperationResult();
                }

                throw new ComputeSystemOperationException(ComputeSystemOperations.Restart);
            }
            catch (Exception ex)
            {
                StateChanged(this, GetState());
                _log.Error(ex, OperationErrorString(ComputeSystemOperations.Restart));
                return new ComputeSystemOperationResult(ex, OperationErrorUnknownString, ex.Message);
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
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.CpuCount, ProcessorCount),
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.AssignedMemorySizeInBytes, MemoryAssigned),
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.StorageSizeInBytes, totalDiskSize),
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.UptimeIn100ns, Uptime),
                };

                var lastCheckPoint = _hyperVManager.GetVirtualMachineCheckpoints(VmId).LastOrDefault();

                if (lastCheckPoint != null)
                {
                    properties.Add(ComputeSystemProperty.CreateCustom(lastCheckPoint.Name, _stringResource.GetLocalized(_currentCheckpointKey), null));
                }

                return properties.AsEnumerable();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"GetComputeSystemPropertiesAsync failed: VM details: {this}");
                return new List<ComputeSystemProperty>();
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson)
    {
        // This is temporary until we have a proper implementation for this.
        var notImplementedException = new NotImplementedException($"Method not implemented by Hyper-V Compute Systems: VM details: {this}");
        return Task.FromResult(new ComputeSystemOperationResult(notImplementedException, OperationErrorUnknownString, notImplementedException.Message)).AsAsyncOperation();
    }

    public SDK.ApplyConfigurationResult ApplyConfiguration(ApplyConfigurationOperation operation)
    {
        // The UX will dictate the actual number of re-tries and will return "cancel" after max. Just in case we'll set a max here too.
        const int MaxRetryAttempts = 7;

        try
        {
            // Start VM first. Hyper-V KVP service can set succeed even if VM is not running and VM will receive
            // registry key changes next time it starts.
            var startResult = Start(string.Empty);
            if (startResult.Result.Status == ProviderOperationStatus.Failure)
            {
                return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(
                    startResult.Result.ExtendedError.HResult,
                    startResult.Result.DisplayMessage,
                    startResult.Result.DiagnosticText));
            }

            using var guestSession = new GuestKvpSession(Guid.Parse(Id));

            // Verify that the VM OS supports configuration operation.
            // Currently only Windows is supported. The minimum requirement is Windows 20H1 (19041)
            ThrowIfApplyConfigurationIsNotSupported(guestSession);

            // Query VM by sending a request to DevSetupAgent.
            var getStateRequest = new GetStateRequest();
            var communicationId = guestSession.SendRequest(getStateRequest, CancellationToken.None);
            var getStateResponses = guestSession.WaitForResponse(communicationId, getStateRequest.RequestId, TimeSpan.FromSeconds(15), false, CancellationToken.None);
            if (getStateResponses.Count > 0)
            {
                var response = getStateResponses[0];
                if (response is GetStateResponse getStateResponse)
                {
                    // Check if VM can accept new Configure requests. We don't support reporting progress for requests
                    // that were started somehow outside of this operation yet. So for now, abort this operation.
                    // This could only happen if Dev Home was restarted while a configuration task was running.
                    if (getStateResponse.StateData.RequestsInQueue.Count > 0)
                    {
                        // Set CommunicationId counter to the value higher than the highest CommunicationId in the queue
                        // to avoid conflicts with previous requests.
                        uint communicationIdCounter = 1; // We've already sent at least one message.
                        foreach (var request in getStateResponse.StateData.RequestsInQueue)
                        {
                            var currentCounter = MessageHelper.GetCounterFromCommunicationId(request.CommunicationId);
                            if (currentCounter > communicationIdCounter)
                            {
                                communicationIdCounter = currentCounter;
                            }
                        }

                        guestSession.SetNextCommunicationIdCounter(communicationIdCounter);

                        _log.Error($"VM is busy with another configuration task. VM details: {this}");
                        return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(HRESULT.E_ABORT, "VM is busy with another configuration task"));
                    }
                }
                else
                {
                    // TODO: Check if we can get any diagnostic from this unexpected response.
                    _log.Error(
                        $"Unexpected response while applying configuration: " +
                        $"responseId: {response.RequestId}, responseType: {response.ResponseType}, " +
                        $"VM details: {this}");
                    return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(HRESULT.E_FAIL, $"Received unexpected response from the VM"));
                }
            }
            else
            {
                // No response from VM. Deploy DevSetupAgent to the VM.
                for (var i = 0; i < MaxRetryAttempts; i++)
                {
                    try
                    {
                        if (!DeployDevSetupAgent(operation, i + 1))
                        {
                            // User canceled the operation.
                            return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(HRESULT.E_ABORT, "User canceled the operation"));
                        }

                        break;
                    }
                    catch (DevSetupAgentDeploymentSessionException ex)
                    {
                        // We couldn't create PS remote session to the VM. Retry to ask for credentials
                        if (i == (MaxRetryAttempts - 1))
                        {
                            return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(ex.HResult, ex.Message));
                        }
                    }
                }
            }

            // Ask VM if there is a logged in user. If not, we need to wait for user to log in.
            for (var i = 0; i < MaxRetryAttempts; i++)
            {
                var userLoggedInRequest = new IsUserLoggedInRequest();
                communicationId = guestSession.SendRequest(userLoggedInRequest, CancellationToken.None);
                var userLoggedInResponses = guestSession.WaitForResponse(communicationId, userLoggedInRequest.RequestId, TimeSpan.FromSeconds(15), false, CancellationToken.None);
                if (userLoggedInResponses.Count > 0)
                {
                    var response = userLoggedInResponses[0];
                    if (response is IsUserLoggedInResponse userLoggedInResponse)
                    {
                        if (!userLoggedInResponse.IsUserLoggedIn)
                        {
                            if (!WaitForUserToLogin(operation, i + 1))
                            {
                                // User canceled the operation.
                                return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(HRESULT.E_ABORT, "User canceled the operation"));
                            }

                            // Send request again to check if user is logged in if we didn't exceed maximum attempt number.
                        }
                        else
                        {
                            // User is logged in. We can continue with configuration.
                            break;
                        }
                    }
                    else
                    {
                        // TODO: Check if we can get any diagnostic from this unexpected response.
                        _log.Error(
                            $"Unexpected response while applying configuration: " +
                            $"responseId: {response.RequestId}, responseType: {response.ResponseType}, " +
                            $"VM details: {this}");
                        return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(HRESULT.E_FAIL, $"Received unexpected response from the VM"));
                    }
                }

                // User is not logged in. We need to wait for user to log in.
                if (i == (MaxRetryAttempts - 1))
                {
                    return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(HRESULT.E_ABORT, "No interactive user on the VM"));
                }
            }

            var configureRequest = new ConfigureRequest(operation.Configuration);
            communicationId = guestSession.SendRequest(configureRequest, CancellationToken.None);

            // Wait for response. 5 hours is an arbitrary period of time that should be big enough
            // for most scenarios.
            // Configuration task can run for a long time that we can't control. What's more, VM can be saved or paused,
            // then started and configuration task will continue to run.
            // TODO: To improve this we can:
            //   make the timeout configurable.
            //   query VM if it has completed the configuration task.
            //   monitor if VM was paused or saved while we are waiting for responses.
            var waitTime = TimeSpan.FromHours(5);
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime) < waitTime)
            {
                var responses = guestSession.WaitForResponse(communicationId, configureRequest.RequestId, TimeSpan.FromSeconds(30), true, CancellationToken.None);

                foreach (var response in responses)
                {
                    if (response is ConfigureResponse configureResponse)
                    {
                        LogApplyConfigurationResult(configureResponse.ApplyConfigurationResult);

                        // Create SDK's result. Set Completed status and event.
                        // Receiving ConfigureResponse means operation has completed. We don't expect anymore responses.
                        return operation.CompleteOperation(configureResponse.ApplyConfigurationResult);
                    }
                    else if (response is ConfigureProgressResponse configureProgressResponse)
                    {
                        LogApplyConfigurationProgress(configureProgressResponse.ProgressData);

                        // Create SDK's result. Set Completed status and event.
                        operation.SetProgress(SDK.ConfigurationSetState.InProgress, configureProgressResponse.ProgressData, null);
                    }
                    else
                    {
                        // Unexpected (error) response. Log it and return error. Not much we can do here.
                        _log.Error(
                            $"Unexpected response while applying configuration: " +
                            $"responseId: {response.RequestId}, responseType: {response.ResponseType}, " +
                            $"VM details: {this}");
                    }
                }
            }

            throw new TimeoutException("Timeout while waiting for the configuration task to complete");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to apply configuration: VM details: {this}");
            return operation.CompleteOperation(new HostGuestCommunication.ApplyConfigurationResult(ex.HResult, ex.Message));
        }
    }

    public IApplyConfigurationOperation? CreateApplyConfigurationOperation(string configuration)
    {
        try
        {
            return new ApplyConfigurationOperation(this, configuration);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to apply configuration: VM details: {this}");
            return new ApplyConfigurationOperation(this, ex);
        }
    }

    private void ThrowIfApplyConfigurationIsNotSupported(GuestKvpSession guestSession)
    {
        Dictionary<string, string>? osVersionInfo = null;
        var isApplyConfigurationSupported = false;

        try
        {
            // The dictionary returned from GetOsVersionInfo is case-insensitive, so the keys are not case-sensitive.
            osVersionInfo = GetOsVersionInfo(guestSession);
            var osPlatformId = int.Parse(osVersionInfo[HyperVStrings.OSPlatformId], CultureInfo.InvariantCulture);
            if (osPlatformId == Constants.GuestPlatformIdWindows)
            {
                var osVersion = Version.Parse(osVersionInfo[HyperVStrings.OSVersion]);
                if (osVersion >= Constants.MinWindowsVersionForApplyConfiguration)
                {
                    isApplyConfigurationSupported = true;
                }
            }
        }
        catch (Exception ex)
        {
            throw new GuestOsVersionException(_stringResource, ex, osVersionInfo);
        }

        if (!isApplyConfigurationSupported)
        {
            throw new GuestOsOperationNotSupportedException(_stringResource, osVersionInfo);
        }
    }

    private Dictionary<string, string> GetOsVersionInfo(GuestKvpSession guestSession)
    {
        var guestOsProperties = guestSession.ReadGuestProperties();
        if (guestOsProperties == null ||
            !guestOsProperties.ContainsKey(HyperVStrings.OSPlatformId) ||
            !guestOsProperties.ContainsKey(HyperVStrings.OSVersion))
        {
            throw new HResultException(HRESULT.E_UNEXPECTED, _stringResource.GetLocalized("FailedToReadGuestOsProperties"));
        }

        return guestOsProperties;
    }

    private bool DeployDevSetupAgent(ApplyConfigurationOperation operation, int attemptNumber)
    {
        var powerShell = _host.GetService<IPowerShellService>();
        var credentialsAdaptiveCardSession = new VmCredentialAdaptiveCardSession(_host, operation, attemptNumber);

        operation.SetProgress(SDK.ConfigurationSetState.WaitingForAdminUserLogon, null, credentialsAdaptiveCardSession);

        (var userName, var password) = credentialsAdaptiveCardSession.WaitForCredentials();

        if ((userName != null) && (password != null))
        {
            var deploymentHelper = new DevSetupAgentDeploymentHelper(powerShell, Id);
            deploymentHelper.DeployDevSetupAgent(userName, password);
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool WaitForUserToLogin(ApplyConfigurationOperation operation, int attemptNumber)
    {
        // Ask user to login to the VM and wait for confirmation.
        var waitForLoginAdaptiveCardSession = new WaitForLoginAdaptiveCardSession(_host, operation, attemptNumber);

        operation.SetProgress(SDK.ConfigurationSetState.WaitingForUserLogon, null, waitForLoginAdaptiveCardSession);

        return waitForLoginAdaptiveCardSession.WaitForUserResponse();
    }

    // TODO: This can be to noisy. We need "verbose" logging level for this.
    private void LogApplyConfigurationProgress(HostGuestCommunication.ConfigurationSetChangeData progressData)
    {
    }

    private void LogApplyConfigurationResult(HostGuestCommunication.ApplyConfigurationResult applyConfigurationResult)
    {
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM Id: {Id} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM Name: {DisplayName} ");
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
        if (operation == ComputeSystemOperations.Delete)
        {
            return $"Failed to complete {operation}: for VM {DisplayName}";
        }

        return $"Failed to complete {operation}: VM details: {this}";
    }

    private string OperationSuccessString(ComputeSystemOperations operation)
    {
        if (operation == ComputeSystemOperations.Delete)
        {
            return $"Successfully completed {operation}: for VM {DisplayName}";
        }

        return $"Successfully completed {operation}: VM details: {this}";
    }
}
