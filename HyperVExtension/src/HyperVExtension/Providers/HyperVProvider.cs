// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Services;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace HyperVExtension.Providers;

/// <summary> Class that provides compute system information for Hyper-V Virtual machines. </summary>
public class HyperVProvider : IComputeSystemProvider
{
    private readonly string errorResourceKey = "ErrorPerformingOperation";

    private readonly IStringResource _stringResource;

    private readonly IHyperVManager _hyperVManager;

    // Temporary will need to add more error strings for different operations.
    public string OperationErrorString => _stringResource.GetLocalized(errorResourceKey);

    public HyperVProvider(IHyperVManager hyperVManager, IStringResource stringResource)
    {
        _hyperVManager = hyperVManager;
        _stringResource = stringResource;
    }

    /// <summary> Gets or sets the default compute system properties. </summary>
    public string DefaultComputeSystemProperties { get; set; } = string.Empty;

    /// <summary> Gets the display name of the provider. This shouldn't be localized. </summary>
    public string DisplayName { get; } = HyperVStrings.HyperVProviderDisplayName;

    /// <summary> Gets the ID of the Hyper-V provider. </summary>
    public string Id { get; } = HyperVStrings.HyperVProviderId;

    /// <summary> Gets the properties of the provider. </summary>
    public string Properties { get; private set; } = string.Empty;

    /// <summary> Gets the supported operations of the Hyper-V provider. </summary>
    /// TODO: currently only CreateComputeSystem is supported in the SDK. For Hyper-V v1 creation
    /// won't be supported.
    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public Uri? Icon
    {
        get => new(Constants.ExtensionIcon);
        set => throw new NotSupportedException("Setting the icon is not supported");
    }

    /// <summary> Creates a new Hyper-V compute system. </summary>
    /// <param name="options">Optional string with parameters that the Hyper-V provider can recognize</param>
    public ICreateComputeSystemOperation? CreateComputeSystem(IDeveloperId developerId, string options)
    {
        // This is temporary until we have a proper implementation for this.
        Logging.Logger()?.ReportError($"creation not supported yet for hyper-v");
        return null;
    }

    /// <summary> Gets a list of all Hyper-V compute systems. The developerId is not used by the Hyper-V provider </summary>
    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            try
            {
                var computeSystems = _hyperVManager.GetAllVirtualMachines();
                Logging.Logger()?.ReportInfo($"Successfully retrieved all virtual machines on: {DateTime.Now}");
                return new ComputeSystemsResult(computeSystems);
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError($"Failed to retrieved all virtual machines on: {DateTime.Now}", ex);
                return new ComputeSystemsResult(ex, OperationErrorString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        // This won't be supported until creation is supported.
        var notImplementedException = new NotImplementedException($"Method not implemented by Hyper-V Compute System Provider");
        return new ComputeSystemAdaptiveCardResult(notImplementedException, OperationErrorString, notImplementedException.Message);
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind)
    {
        // This won't be supported until property modification is supported.
        var notImplementedException = new NotImplementedException($"Method not implemented by Hyper-V Compute System Provider");
        return new ComputeSystemAdaptiveCardResult(notImplementedException, OperationErrorString, notImplementedException.Message);
    }

    // This will be implemented in a future release, but will be available for Dev Environments 1.0.
    public ICreateComputeSystemOperation CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson) => throw new NotImplementedException();
}
