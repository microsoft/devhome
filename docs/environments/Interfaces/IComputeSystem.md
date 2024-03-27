# `IComputeSystem` interface

## Terminology

A **compute system** is considered to be any one of the following:

1. Local machine
2. Virtual machine
3. Remote machine
4. Container

Dev Home uses the [IComputeSystem](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L812) interface to interact with these types of software/hardware systems. 

## Operations supported by `IComputeSystem`

1. Start the system
2. Shut down the system
3. Terminate the system
4. Delete the system
5. Save the state of the system
6. Pause the system
7. Resume the system
8. Restart the system
9. Create a Snapshot of the system
10. Revert to the last Snapshot of the system
11. Delete a Snapshot on the system
12. Apply a configuration file onto a system
13. Modify the properties of the system

## Implementing `IComputeSystem`

Extension developers should create and return an `IComputeSystem` for every compute system they want Dev Home to interact with. The operations listed above can be performed by the interface. Extension developers can limit which operations each `IComputeSystem` supports by utilizing the [ComputeSystemOperations](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L510) enum.

Extensions should create objects that represent each compute system that their underlying platform manages. These objects must: 
1. Implement the `IComputeSystem` interface
2. Contain data about the software/hardware system and have methods to interact with them. 

### Example

The [Hyper-V extension](https://github.com/microsoft/devhome/tree/main/HyperVExtension) interacts directly with the Hyper-V virtual machine management service to perform the above operations on the virtual machine. 

To provide Dev Home the ability to initiate one of these operations, we have created the `IComputeSystem` interface. Extensions are expected to map their software/hardware system to the `IComputeSystem` interface and send these operations back to Dev Home. Dev Home can initiate one of these operations via a method call in the `IComputeSystem` interface, based on user input. The extension can then interact with its underlying hardware/software system to perform that operation.

### Why do some of the `IComputeSystem` methods take an `options` string parameter

Since the extension interacts with Dev Home and not the user directly, there may be cases where an extension may want to customize the behavior of the method based on user input. 

An extension can provide Dev Home with an [Adaptive card](https://learn.microsoft.com/adaptive-cards/) (with an action button).

1. When the action button is invoked, a JSON string is generated with user input
2. This user input can then be passed to a method in the extension
3. The extension can then use this input when performing an operation. 

Since the extension is the originator of the adaptive card, it will know what key/value pairs to expect within the JSON, and can deserialize it accordingly. Dev Home is simply an intermediary between the extension and the user, with its only role being to:

1. Render the [Adaptive card](https://learn.microsoft.com/adaptive-cards/) for the extension
2. Pass the inputs to appropriate interface method.

**Note:** It is not required to implement the interface method and use the options string. It is optional for all the operations of the `IComputeSystem`, however your `ModifyPropertiesAsync` method implementation will likely need to utilize it to know which properties to modify. These properties can be different from what is sent via the `IComputeSysten.GetComputeSystemPropertiesAsync` method.

## Detailed Examples

### Implementing a class that implements IComputeSystem

```CS

/// <summary> Helper C# Class for strings related to Hyper-V</summary>
public static class HyperVStrings
{
    // Common strings used for Hyper-V extension
    public const string HyperVProviderDisplayName = "Microsoft Hyper-V";
    public const string HyperVProviderId = "Microsoft.HyperV";
    public const string WindowsThumbnail = "ms-appx:///HyperVExtension/Assets/hyper-v-windows-default-image.jpg";

    // Hyper-V psObject property values
    public const string CanStopService = "CanStop";
    public const string VMOffState = "Off";
    public const string RunningState = "Running";
    public const string PausedState = "Paused";
    public const string SavedState = "Saved";
}

/// <summary> C# Class that represents a Hyper-V virtual machine object. </summary>
public class HyperVVirtualMachine : IComputeSystem
{
    #region Hyper-V-Specific-Functionality-for-class

    /// <summary>
    /// Object this class interacts with to perform operations on VMs within the Hyper-V
    /// platform.
    /// </summary>
    private readonly IHyperVManager _hyperVManager;

    /// <summary>
    /// Property of virtual machine to show in the UI
    /// Represents 4 Gigabytes in bytes.
    /// </summary>
    public long MemoryAssigned => 4294967296; 

    /// <summary>
    /// Property of virtual machine to show in the UI.
    /// Represents a timespan of 1 day, 2 hours, and 1 minute
    /// </summary>
    public TimeSpan Uptime => new TimeSpan(1, 2, 1, 0, 0);

    /// <summary>
    /// Property of virtual machine to show in the UI.
    /// Extensions can get the state of their software/hardware system any way they
    /// want but they need to map their states to the enum provided in Dev Homes 
    /// SDK. See ComputeSystemState here:
    /// https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L530
    /// </summary>
    public string State => _hyperVManager.GetVmState(Id);

    /// <summary>
    /// Property of virtual machine to show in the UI.
    /// </summary>
    public long ProcessorCount => 4;

    /// <summary>
    /// Property of virtual machine to show in the UI.
    /// We'll use this to make a custom property, where we'll show the current
    /// checkpoint in Dev Homes UI.
    /// </summary>
    public string ParentCheckpointName => "Before Windows update Checkpoint";

    /// <summary>
    /// We'll use this when attempting to revert a checkpoint. This is hyper-v
    /// specific.
    /// </summary>
    public Guid ParentCheckpointId => "5e05074f-eb12-4069-9290-e183c9ea31c4"

    /// <summary>
    /// Helper method to get the total storage of all virtual disk attached
    /// to the virtual machine. We'll use this to show the amount of storage
    /// the virtual machine has in Dev Homes UI.
    /// </summary>
    public ulong GetTotalStorage()
    {
        ulong diskSize = 0;
        foreach (var hardDrive in GetVmHardDriveList())
        {
            diskSize += _hyperVManager.GetVhdSize(hardDrive.Path);            
        }

        return diskSize;
    }

    public HyperVVirtualMachine(IHyperVManager hyperVManager)
    {
        _hyperVManager = hyperVManager;
    }

    #endregion Hyper-V-Specific-Functionality-for-class

    #region IComputeSystem-Specific-Functionality-for-class

    /// <summary>
    /// Implementing the Id property. Id's should be strings and 
    /// should be Unique per compute system within an Extension.
    /// Extensions don't need to worry about these needing to be
    /// globally unique but it is recommended. For Hyper-V virtual machines
    /// for example we get back a Guid from the hyper-V platform and use
    /// that as the Id for the compute system.
    /// </summary>
    public string Id => "7e05077f-eb12-4069-9290-e185cbfb31c4";

    /// <summary>
    /// We'll display this string in Dev Homes UI. It is recommended
    /// for production for these to be localized.
    /// </summary>
    public string DisplayName => "VM for testing";

    /// <summary>
    /// Dev Home will subscribe to this event to receive state changes for the compute system
    /// </summary>
    public event TypedEventHandler<IComputeSystem, ComputeSystemState> StateChanged = (s, e) => { };

    /// <summary>
    /// The extension dictates which operations a compute system supports. So we can choose to
    /// not include any compute system operation we don't want to support. Dev Home will use
    /// these flags to know which operations to display in Its UI for the compute system.
    /// E.g Here in this example we have chosen not to support the ApplyConfiguration and ModifyProperties
    /// operations.
    /// </summary>
    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Start |
                ComputeSystemOperations.ShutDown |
                ComputeSystemOperations.Terminate |
                ComputeSystemOperations.Delete |
                ComputeSystemOperations.Save |
                ComputeSystemOperations.Pause |
                ComputeSystemOperations.Resume |
                ComputeSystemOperations.CreateSnapshot |
                ComputeSystemOperations.DeleteSnapshot |
                ComputeSystemOperations.Restart;

    /// <summary>
    /// The SDK allows compute systems to provide a supplemental name. It is not mandatory
    /// for this name to be non-empty. This is a useful way to further separate your
    /// compute systems within Dev Homes UI.
    /// </summary>
    public string SupplementalDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// It is not mandatory for the AssociatedDeveloperId to be non-null, as IDeveloperIds
    /// are mostly used for cloud scenarios. Whereas in the local scenario there may not
    /// be a need to use one.
    /// </summary>
    public IDeveloperId? AssociatedDeveloperId { get; set; }

    // The Id of the provider that the compute system was retrieved from.
    public string AssociatedProviderId { get; set; } = HyperVStrings.HyperVProviderId;
    
    /// <summary>
    /// Asynchronously retrieves the state of the compute system.
    /// </summary>
    /// <returns>An asynchronous operation that returns the compute system state.</returns>
    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task.Run(() =>
        {
            // Determine the current state based on the State property.
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
    /// Asynchronously starts the virtual machine if it is not already running.
    /// </summary>
    /// <param name="options">The options for starting the VM, not used in this example.</param>
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
            if (_hyperVManager.StartVirtualMachine(Id))
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
    /// Asynchronously initiates the shutdown of the virtual machine if it is currently running.
    /// </summary>
    /// <param name="options">The options for shutting down the VM, not used in this example.</param>
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
                if (_hyperVManager.StopVirtualMachine(Id, StopVMKind.Default))
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
    /// Asynchronously forces the termination of the virtual machines power state if it is currently running.
    /// </summary>
    /// <param name="options">The options for terminating the VM, not used in this example.</param>
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
                if (_hyperVManager.StopVirtualMachine(Id, StopVMKind.TurnOff))
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
    /// <param name="options">The options for deleting the VM, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the delete operation.</returns>
    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // Tell Dev Home we're deleting the compute system
                StateChanged(this, ComputeSystemState.Deleting);
                if (_hyperVManager.RemoveVirtualMachine(Id))
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
    /// <param name="options">The options for saving the VM, not used in this example.</param>
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
                if (_hyperVManager.StopVirtualMachine(Id, StopVMKind.Save))
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
    /// <param name="options">The options for pausing the VM, not used in this example.</param>
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
                if (_hyperVManager.PauseVirtualMachine(Id))
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
    
    /// <summary>
    /// Asynchronously resumes the virtual machine.
    /// </summary>
    /// <param name="options">The options for resuming the VM, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the resume operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // If already running don't attempt to resume the VM.
                if (State == HyperVStrings.RunningState)
                {
                    // VM is already in running state so return successful result.
                    return new ComputeSystemOperationResult();
                }

                // Tell Dev Home we're starting the compute system
                StateChanged(this, ComputeSystemState.Starting);
                if (_hyperVManager.ResumeVirtualMachine(Id))
                {
                    // operation succeeded so update state to running
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

    /// <summary>
    /// Asynchronously creates a snapshot of the virtual machine.
    /// </summary>
    /// <param name="options">The options for creating the VM snapshot, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the create snapshot operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // Attempt to create the snapshot. Note there is no state update here.
                // The state update is intended to be used for operations that change
                // the state of the compute system itself.
                if (_hyperVManager.CreateCheckpoint(Id))
                {
                    // Snapshot creation successful so return success result
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.CreateSnapshot);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll send the failure ComputeSystemOperationResult 
                // back to Dev Home who can then show this failure in the UI.
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously reverts a previous snapshot of the virtual machine.
    /// </summary>
    /// <param name="options">The options for reverting back to the previous VM snapshot, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the revert snapshot operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // Reverting checkpoints means applying the previous checkpoint onto the VM.
                // Note there is no state update here.
                // The state update is intended to be used for operations that change
                // the state of the compute system itself.
                if (_hyperVManager.ApplyCheckpoint(Id, ParentCheckpointId))
                {
                    // Snapshot successfully reverted so we'll send back the success
                    // result
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.RevertSnapshot);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll send the failure ComputeSystemOperationResult 
                // back to Dev Home who can then show this failure in the UI.
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously deletes a snapshot of the virtual machine.
    /// </summary>
    /// <param name="options">The options for deleting the VM snapshot, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the delete snapshot operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // We can use the options string to receive which checkpoint the user wants to delete
                // from Dev Home but for this example we'll remove the previous one.
                if (_hyperVManager.RemoveCheckpoint(Id, ParentCheckpointId))
                {
                    // Snapshot successfully deleted so we'll send back the success
                    // result
                    return new ComputeSystemOperationResult();
                }

                throw new HyperVOperationException(ComputeSystemOperations.DeleteSnapshot);
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll send the failure ComputeSystemOperationResult 
                // back to Dev Home who can then show this failure in the UI.
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously connects to a virtual machine.
    /// </summary>
    /// <param name="options">The options for connecting to the virtual machine, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the connect operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // We'll kick off the operation and only send a failure if there were any exceptions.
                _hyperVManager.ConnectToVirtualMachine(Id);
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                // Operation failure occured so we'll send the failure ComputeSystemOperationResult 
                // back to Dev Home who can then show this failure in the UI.
                return new ComputeSystemOperationResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously retarts a virtual machine.
    /// </summary>
    /// <param name="options">The options for restarting the virtual machine, not used in this example.</param>
    /// <returns>A ComputeSystemOperationResult indicating the result of the restart operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                // If the VM is not running don't attempt to restart it.
                if (State != HyperVStrings.RunningState)
                {
                    throw new HyperVOperationException(ComputeSystemOperations.Restart);
                }

                // Tell Dev Home we're restarting the compute system
                StateChanged(this, ComputeSystemState.Restarting);
                if (_hyperVManager.RestartVirtualMachine(Id))
                {
                    // operation succeeded so update state to running
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

    /// <summary>
    /// Asynchronously retrieves the thumbnail image of the compute system.
    /// </summary>
    /// <param name="options">The options for retrieving the thumbnail, not used in this example.</param>
    /// <returns>A ComputeSystemThumbnailResult indicating the result of the operation, this contains the thumbnail</returns>
    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        return Task.Run(async () =>
        {
            try
            {
                // Get image file from package and convert it to a stream
                var uri = new Uri(HyperVStrings.WindowsThumbnail);
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
                var randomAccessStream = await storageFile.OpenReadAsync();

                // Convert the stream to a byte array and pass this back to Dev Home,
                // who will then deserialize it and display the image in the UI.
                var bytes = new byte[randomAccessStream.Size];
                await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);
                return new ComputeSystemThumbnailResult(bytes);
            }
            catch(Exception ex)
            {
                // Operation failure occured so we'll send the failure ComputeSystemOperationResult 
                // back to Dev Home who can then show this failure in the UI.
                return new ComputeSystemThumbnailResult(ex, "Something went wrong", ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Asynchronously retrieves a collection of compute system properties.
    /// </summary>
    /// <param name="options">The options for retrieving the properties, not used in this example.</param>
    /// <returns>An asynchronous operation that returns an enumerable of compute system properties.</returns>
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

                    // Create custom property for the current checkpoint. For this we need a value
                    // and a name. We don't have an icon, so we'll leave that parameter as null.
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

    /// <summary>
    /// Asynchronously modifies one or more compute system properties.
    /// </summary>
    /// <param name="inputJson">A user input json string, not used in this example.</param>
    /// <returns>An asynchronous operation that returns the result of the modify properties operation</returns>
    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson)
    {
        // This is not implemented but to make sure Dev Home never attempts to call it the extension
        // must not add the ModifyProperties enum flag to the Compute systems SupportedOperations,
        // like we have done above.
        var exception = new NotImplementedException($"Method not implemented");
        return Task.FromResult(new ComputeSystemOperationResult(notImplementedException, "Something went wrong", exception.Message)).AsAsyncOperation();
    }

    /// <summary>
    /// Creates an operation to apply the contents of a configuration file onto a compute system.
    /// </summary>
    /// <param name="configuration">The contents of a configuration file, not used in this example.</param>
    /// <returns>An asynchronous operation that returns the result of the modify properties operation</returns>
    public IApplyConfigurationOperation? CreateApplyConfigurationOperation(string configuration)
    {
        // This is unimplemented. Dev Home will handle cases where this null, however
        // it is on the extension to make sure it does not add the ApplyConfiguration enum
        // flag to the Compute systems SupportedOperations, like we have done above.
        return null;
    }

    #endregion IComputeSystem-Specific-Functionality-for-class
}
```
