# What is the `IComputeSystemProvider` interface

A **compute system provider** is the provider type that Dev Home will query when initially interacting with an environment extension. The compute system provider is used to perform general operations that are not specific to a compute system. Extension developers should implement the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L458) to perform the following operations:

1. Retrieve a list of `IComputeSystem`s
2. Create a new compute system
3. Provide Dev Home with an [Adaptive card](https://learn.microsoft.com/adaptive-cards/) for the creation of a new compute system
4. Provide Dev Home with an Adaptive card for the modification of a compute systems properties

Dev Home will look at the [ComputeSystemProviderOperations](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L461) enum to determine what operations the provider supports.

## Examples

### Implementing a class that implements IComputeSystemProvider

```CS

/// <summary>
/// Handles the adaptive card session for creating a compute system
/// </summary>
public class VmGalleryAdaptiveCardSession : IExtensionAdaptiveCardSession2 
{
    // ... variable properties and methods that implement IExtensionAdaptiveCardSession2 
}

/// <summary>
/// Handles the adaptive card session for modifying compute system properties
/// </summary>
public class ModifyComputeSystemPropertiesAdaptiveCardSession : IExtensionAdaptiveCardSession2 
{
    private readonly IComputeSystem _computeSystem;

    // ... variable properties and methods that implement IExtensionAdaptiveCardSession2 

    public ModifyComputeSystemPropertiesAdaptiveCardSession(IComputeSystem computeSystem)
    {
        _computeSystem = computeSystem;
    }
}

/// <summary>
/// Represents the user input for the creation operation.
/// </summary>
public sealed class UserCreationInput
{
    public string NewVirtualMachineName { get; set; } = string.Empty;

    public int SelectedImageListIndex { get; set; }
}

/// <summary>
/// Class that represents an operation to create a compute system.
/// see: https://github.com/microsoft/devhome/blob/4fe92fa0fdb2f0c3bea3c4b906241e85c21012e4/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L467
/// </summary>
public sealed class CreateComputeSystemOperation : ICreateComputeSystemOperation
{
    private readonly UserCreationInput _userInput;

    // ... various other properties and methods that implement ICreateComputeSystemOperation

    public CreateComputeSystemOperation(UserCreationInput input)
    {
        _userInput = input;
    }
}

/// <summary> 
/// Class that provides compute system information for Hyper-V Virtual machines.
/// </summary>
public class HyperVProvider : IComputeSystemProvider
{
    #region Hyper-V-Specific-Functionality-for-class

    /// <summary>
    /// Object this class interacts with to perform operations on VMs
    /// within the Hyper-V
    /// platform.
    /// </summary>
    private readonly IHyperVManager _hyperVManager;

    public HyperVProvider(IHyperVManager hyperVManager)
    {
        _hyperVManager = hyperVManager;
    }

    #endregion Hyper-V-Specific-Functionality-for-class

    #region IComputeSystemProvider-Specific-Functionality-for-class

    /// <summary>
    /// Gets the display name of the provider.
    /// </summary>
    public string DisplayName { get; } = "Microsoft Hyper-V";

    /// <summary>
    /// Gets the Id of the Hyper-V provider.
    /// </summary>
    public string Id { get; } = "Microsoft.Hyper-V";

    /// <summary>
    ///  Gets the supported operations of the Hyper-V provider.
    /// </summary>
    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    // provide the Icon for the provider. Note the format:
    // see comments here regarding format: https://github.com/microsoft/devhome/blob/4fe92fa0fdb2f0c3bea3c4b906241e85c21012e4/HyperVExtension/src/HyperVExtension/Constants.cs#L10
    public Uri Icon => new("ms-resource://Microsoft.Windows.DevHome/Files/HyperVExtension/Assets/hyper-v-provider-icon.png")

    /// <summary> Gets a list of all Hyper-V compute systems. The developerId is not used by the Hyper-V provider </summary>
    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            try
            {
                // Hyper-V Manager returns a list of IComputeSystems
                var computeSystems = _hyperVManager.GetAllVirtualMachines();
                return new ComputeSystemsResult(computeSystems);
            }
            catch (Exception ex)
            {
                return new ComputeSystemsResult(ex, OperationErrorString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Gets the adaptive card session for the specified compute
    /// system adaptive card kind. The adaptive card session is used
    /// by Dev Home to display UI from the extension to the user. The
    /// ending result of the json provided by the session can then be
    /// used by the extension to create a compute system for example. 
    /// </summary>
    /// <param name="developerId">
    /// The developer Id to associate with the adaptive card. Used in
    /// this example.
    /// </param>
    /// <param name="sessionKind">
    /// An The adaptive card session enum: see https://github.com/microsoft/devhome/blob/4fe92fa0fdb2f0c3bea3c4b906241e85c21012e4/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L451
    /// </param>
    /// <returns>
    /// A result that contains an adaptive card session when successful
    /// and an error when unsuccessful.
    /// </returns>
    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        if (sessionKind == ComputeSystemAdaptiveCardKind.CreateComputeSystem)
        {
            return return new ComputeSystemAdaptiveCardResult(new VmGalleryAdaptiveCardSession());
        }
        
         var exception = new NotImplementedException($"Unsupported ComputeSystemAdaptiveCardKind");
 return new ComputeSystemAdaptiveCardResult(exception, exception.Message, exception.Message);

    }

    /// <summary>
    /// Gets the adaptive card session for the specified compute
    /// system adaptive card kind. The adaptive card session is used
    /// by Dev Home to display UI from the extension to the user. The
    /// ending result of the json provided by the session can then be
    /// used by the extension to modify specific properties of a compute
    /// system for example. 
    /// </summary>
    /// <param name="computeSystem">
    /// The compute system that will be related to the adaptive card
    /// session. 
    /// </param>
    /// <param name="sessionKind">
    /// An The adaptive card session enum: see https://github.com/microsoft/devhome/blob/4fe92fa0fdb2f0c3bea3c4b906241e85c21012e4/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L451
    /// </param>
    /// <returns>
    /// A result that contains an adaptive card session when successful
    /// and an error when unsuccessful.
    /// </returns>
    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind)
    {
        if (sessionKind == ModifyComputeSystemProperties)
        {
            return new ComputeSystemAdaptiveCardResult(new ModifyComputeSystemPropertiesAdaptiveCardSession(computeSystem));
        }

         var exception = new NotImplementedException($"Unsupported ComputeSystemAdaptiveCardKind");
 return new ComputeSystemAdaptiveCardResult(exception, exception.Message, exception.Message);
    }

    /// <summary> Creates a new Hyper-V compute system operation.
    /// The operation can then be started with the StartAsync method
    /// of the ICreateComputeSystemOperation interface. 
    /// </summary>
    /// <param name="options">
    /// The input json used to create the compute system. This should
    /// be json the provider knows how to interpret. 
    /// </param>
    /// <returns>
    /// The creation operation that can be started by the caller
    ///</returns>
    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId? developerId, string inputJson)
    {
        try
        {
            // using the .Net JsonSerializer to deserialize the user input
            // into a class object defined above.
            if (JsonSerializer.Deserialize(inputJson, typeof(UserCreationInput)) is not UserCreationInput userInput)
            {
                throw new InvalidOperationException($"Failed to deserialize the input json {inputJson} to {nameof(UserCreationInput)} object.");
            }

            return CreateComputeSystemOperation(userInput);
        }
        catch (Exception ex)
        {
            // Dev Home will handle null values as failed operations. We can't throw because this is an out of proc
            // COM call, so we'll lose the error information. We'll log the error and return null.
            return null;
        }
    }

    #endregion IComputeSystemProvider-Specific-Functionality-for-class
}
```
