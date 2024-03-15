# What are Dev Environments in Dev Home

Dev Environments is the name Dev Home holistically gives a compute system which can contain dev projects. These projects contain apps, packages and cloned repositories. The goal of Dev Environments is for developers to have a one stop shop for all their environments in a single place.

## Terminology

**Compute System:** A compute system is considered to be any one of the following:

1. Local machine
1. Virtual machine
1. Remote machine
1. Container

Dev Home uses the [IComputeSystem](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L757) interface to interact with these types of software/Hardware systems. Extension developers should create and return an `IComputeSystem` for every compute system they want Dev Home to interact with. The following management operations can be performed by the interface.

Operations:

1. Start the system,
1. ShutDown the system,
1. Terminate the system,
1. Delete the system,
1. Save the state of the system,
1. Pause the system,
1. Resume the system,
1. Restart the system,
1. Create a Snapshot of the system,
1. Revert to the last Snapshot of the system,
1. Delete a Snapshot on the system,
1. Apply a configuration file onto a system,
1. Modify the properties of the system

Note: Extension developers can limit which operations an `IComputeSystem` supports by utilizing the [ComputeSystemOperations](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L510) enum.

Extensions should create objects that represent each compute system that their underlaying platform manages. These objects would implement the `IComputeSystem` interface, contain data about the object and have methods to interact with the underlaying platform. For example, The Hyper-V extension interacts directly with the Hyper-V virtual machine management service to perform the above operations under the hood. However, to provide Dev Home the ability to initiate one of these operations, we have created the `IComputeSystem` interface. Extensions create their objects, map them to the `IComputeSystem` interface and send these back to Dev Home. Dev Home can initiate one of these operation via a method call in the `IComputeSystem` interface, based on user input and then the extension can interact with its underlaying platform to perform that operation.  

**Compute system provider:** A compute system provider is the provider type that Dev Home will query for when initially interacting with an extension. The compute system provider is used to perform general operations that are not specific to a ComputeSystem. Extension developers should implement the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L458) to perform the following operations:

1. Retreive a list of `IComputeSystem`s
1. Create a `IComputeSystem`
1. Provide Dev Home with an Adaptive card for the creation of an `IComputeSystem`
1. Provide Dev Home with an Adaptive card for the modification of an `IComputeSystem`s properties

Note: Only the creation operation can be limited by utilizing the [ComputeSystemProviderOperations](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L406) enum.

## What is needed to create a Dev Environment extension?

First take a look at Dev Homes extensions documentation [here](https://github.com/microsoft/devhome/blob/main/docs/extensions.md)
Then see the sample extension documentation Dev Homes sample extension [here](https://github.com/microsoft/devhome/tree/main/extensions/SampleExtension)

Your extension will need to do three things to be considered an Extension that Dev Home recognizes as being a Dev Environment extension.

1. Within the `Package.appxmanifest` file for the package, the `<ComputeSystem />`  attribute should be added to the list of the extensions supported interfaces. See: Dev Home's [appxmanifest file](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/src/Package.appxmanifest#L75)

1. Your extension should implement the [IExtension](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L7) interface and within its `GetProvider` method return a single `IComputeSystemProvider` or a list of `IComputeSystemProvider`s

1. A class that implements the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L458) interface should be created. This interface is used by Dev Home to perform specific operations like retrieving a list of [IComputeSystem](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L757) interfaces and creating a compute system.

## Examples

### Implementing a class that implements IComputeSystem

```CS
/// <summary> C# Class that represents a Hyper-V virtual machine object. </summary>
public class HyperVVirtualMachine : IComputeSystem
{
    // Object this class interacts with to perform operations on VMs within the Hyper-V
    // platform.
    private readonly IHyperVManager _hyperVManager;

    public string Id => "7e05077f-eb12-4069-9290-e185cbfb31c4";

    public string DisplayName => "VM for testing";

    public long MemoryAssigned => 4096; // 4 Gb

    public TimeSpan Uptime => new TimeSpan(1, 2, 1, 0, 0); //  1 day, 2 hours, and 1 minute

    public string State => "Running";

    public long ProcessorCount => 4; // 4 vCPUs

    public string ParentCheckpointName => "Before Windows update Checkpoint";

    // Dev Home will subscribe to this event to receive state changes for the compute system
    public event TypedEventHandler<IComputeSystem, ComputeSystemState> StateChanged = (s, e) => { };

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

    public string SupplementalDisplayName { get; set; } = string.Empty;

    public IDeveloperId? AssociatedDeveloperId { get; set; }

    public string AssociatedProviderId { get; set; } = "Microsoft.HyperV";

    public HyperVVirtualMachine(IHyperVManager hyperVManager)
    {
        _hyperVManager = hyperVManager;
    }

    public ulong GetTotalStorage()
    {
        ulong diskSize = 0;
        foreach (var hardDrive in GetVmHardDriveList())
        {
            diskSize += _hyperVManager.GetVhdSize(hardDrive.Path);            
        }

        return diskSize;
    }

    
    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
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

    /// <summary>
    /// Starts the virtual machine if it is not already running.
    /// </summary>
    /// <param name="options">The options for starting the VM, currently not used.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the start operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options)
    {
        try
        {
            // If already running don't attempt to start it.
            if (State == HyperVStrings.RunningState)
            {
                // VM is already running so return successful result.
                return new ComputeSystemOperationResult();
            }

            // Tell Dev Home we're starting this compute system
            StateChanged(this, ComputeSystemState.Starting);
            if (_hyperVManager.StartVirtualMachine(VmId))
            {
                // operation succeeded so update state to running
                StateChanged(this, ComputeSystemState.Running);
                return new ComputeSystemOperationResult();
            }

            throw new HyperVOperationException(ComputeSystemOperations.Start);
        }
        catch (Exception ex)
        {
            // Operation failure occured so we'll make the state unknown and send the failure
            // ComputeSystemOperationResult back to Dev Home who can then show this failure in
            // the UI.
            StateChanged(this, ComputeSystemState.Unknown);
            return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
        }
    }

    /// <summary>
    /// Initiates a shutdown of the virtual machine if it is currently running.
    /// </summary>
    /// <param name="options">The options for shutting down the VM, currently not used.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the shutdown operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // If already off don't attempt to turn it off.
                if (State == HyperVStrings.VMOffState)
                {
                    // VM is already off so return successful result.
                    return new ComputeSystemOperationResult();
                }

                // Tell Dev Home we're stopping the compute system
                StateChanged(this, ComputeSystemState.Stopping);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.Default))
                {
                    // operation succeeded so update state to Stopped
                    StateChanged(this, ComputeSystemState.Stopped);
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.ShutDown);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Forcibly terminates the virtual machine if it is currently running.
    /// </summary>
    /// <param name="options">The options for terminating the VM, currently not used.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the terminate operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // If already off don't attempt to turn it off.
                if (State == HyperVStrings.VMOffState)
                {
                    // VM is already off so return successful result.
                    return new ComputeSystemOperationResult();
                }

                // Tell Dev Home we're stopping the compute system
                StateChanged(this, ComputeSystemState.Stopping);

                // This particular method turns off the virtual machine, without
                // gracefully shutting it down. Terminate operations should be considered
                // the equivalent to pull the plug out of a running computer.
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.TurnOff))
                {
                    // operation succeeded so update state to Stopped
                    StateChanged(this, ComputeSystemState.Stopped);
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.Terminate);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously deletes the virtual machine.
    /// </summary>
    /// <param name="options">The options for deleting the VM, currently not used.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the delete operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // Tell Dev Home we're deleting the compute system
                StateChanged(this, ComputeSystemState.Deleting);
                if (_hyperVManager.RemoveVirtualMachine(VmId))
                {
                    // operation succeeded so update state to deleted
                    StateChanged(this, ComputeSystemState.Deleted);
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.Delete);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously saves the current state of the virtual machine.
    /// </summary>
    /// <param name="options">The options for saving the VM, currently not used.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the save operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // If already in the saved state don't attempt to save the state again.
                if (State == HyperVStrings.SavedState)
                {
                    // VM is already in saved state so return successful result.
                    return new ComputeSystemOperationResult();
                }

                // Tell Dev Home we're saving the compute system's state
                StateChanged(this, ComputeSystemState.Saving);
                if (_hyperVManager.StopVirtualMachine(VmId, StopVMKind.Save))
                {
                    // operation succeeded so update state to saved
                    StateChanged(this, ComputeSystemState.Saved);
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.Save);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously pauses the virtual machine.
    /// </summary>
    /// <param name="options">The options for pausing the VM, currently not used.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the pause operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // If already paused don't attempt to pause the VM.
                if (State == HyperVStrings.PausedState)
                {
                    // VM is already in paused state so return successful result.
                    return new ComputeSystemOperationResult();
                }

                // Tell Dev Home we're pausing the compute system
                StateChanged(this, ComputeSystemState.Pausing);
                if (_hyperVManager.PauseVirtualMachine(VmId))
                {
                    // operation succeeded so update state to paused
                    StateChanged(this, ComputeSystemState.Paused);
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.Pause);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.Resume);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.CreateSnapshot);
            }
            catch (Exception ex)
            {
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.RevertSnapshot);
            }
            catch (Exception ex)
            {
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.DeleteSnapshot);
            }
            catch (Exception ex)
            {
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                    throw new HyperVOperationException(ComputeSystemOperations.Restart);
                }

                StateChanged(this, ComputeSystemState.Restarting);
                if (_hyperVManager.RestartVirtualMachine(VmId))
                {
                    StateChanged(this, ComputeSystemState.Running);
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.Restart);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll make the state unknown and send the failure
                // ComputeSystemOperationResult back to Dev Home who can then show this failure in
                // the UI.
                StateChanged(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
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
                // List a few properties that the underlying Hyper-V platform supports.
                var properties = new List<ComputeSystemProperty>
                {
                    // Create predefined properties. Only providing the enum and the value is
                    // necessary.
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.CpuCount, ProcessorCount),
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.AssignedMemorySizeInBytes, MemoryAssigned),
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.StorageSizeInBytes, GetTotalStorage()),
                    ComputeSystemProperty.Create(ComputeSystemPropertyKind.UptimeIn100ns, Uptime),

                    // Create custom property for the current checkpoint. For this we need a name
                    // and a value. We don't have an icon, so we'll leave that parameter as null.
                    ComputeSystemProperty.CreateCustom(ParentCheckpointName, "Current Checkpoint", null),
                };

                return properties.AsEnumerable();
            }
            catch (Exception ex)
            {
                return new List<ComputeSystemProperty>();
            }
        }).AsAsyncOperation();
    }
}
```
