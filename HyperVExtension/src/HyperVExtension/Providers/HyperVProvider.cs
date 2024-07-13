// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.Common;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Models.VirtualMachineCreation;
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

    private readonly VmGalleryCreationOperationFactory _vmGalleryCreationOperationFactory;

    // Temporary will need to add more error strings for different operations.
    public string OperationErrorString => _stringResource.GetLocalized(errorResourceKey);

    public HyperVProvider(IHyperVManager hyperVManager, IStringResource stringResource, VmGalleryCreationOperationFactory vmGalleryCreationOperationFactory)
    {
        _hyperVManager = hyperVManager;
        _stringResource = stringResource;
        _vmGalleryCreationOperationFactory = vmGalleryCreationOperationFactory;
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

    public Uri Icon => new(Constants.ExtensionIcon);

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

    /// <summary> Creates an operation that will create a new Hyper-V virtual machine. </summary>
    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId? developerId, string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, typeof(VMGalleryCreationUserInput));
            var inputForGalleryOperation = deserializedObject as VMGalleryCreationUserInput ?? throw new InvalidOperationException($"Json deserialization failed for input Json: {inputJson}");
            return _vmGalleryCreationOperationFactory(inputForGalleryOperation);
        }
        catch (Exception ex)
        {
            Logging.Logger()?.ReportError($"Failed to create a new virtual machine on: {DateTime.Now}", ex);

            // Dev Home will handle null values as failed operations. We can't throw because this is an out of proc
            // COM call, so we'll lose the error information. We'll log the error and return null.
            return null;
        }
    }
}
