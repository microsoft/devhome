// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.Common;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.Services;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace HyperVExtension.Providers;

/// <summary> Class that provides compute system information for Hyper-V Virtual machines. </summary>
public class HyperVProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(HyperVProvider));

    private readonly string errorResourceKey = "ErrorPerformingOperation";

    private readonly IStringResource _stringResource;

    private readonly IHyperVManager _hyperVManager;

    private readonly WindowsIdentityWrapper _windowsIdentityWrapper;

    private readonly VmGalleryCreationOperationFactory _vmGalleryCreationOperationFactory;

    private readonly IVMGalleryService _vmGalleryService;

    // Temporary will need to add more error strings for different operations.
    public string OperationErrorString => _stringResource.GetLocalized(errorResourceKey);

    public HyperVProvider(
        IHyperVManager hyperVManager,
        IStringResource stringResource,
        IWindowsIdentityService windowsIdentityService,
        VmGalleryCreationOperationFactory vmGalleryCreationOperationFactory,
        IVMGalleryService vmGalleryService)
    {
        _hyperVManager = hyperVManager;
        _stringResource = stringResource;
        _vmGalleryCreationOperationFactory = vmGalleryCreationOperationFactory;
        _vmGalleryService = vmGalleryService;
        _windowsIdentityWrapper = windowsIdentityService.GetCurrentWindowsIdentity();
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
    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public Uri Icon => new(Constants.ExtensionIcon);

    /// <summary> Gets a list of all Hyper-V compute systems. The developerId is not used by the Hyper-V provider </summary>
    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!_windowsIdentityWrapper.IsUserInGroup(HyperVStrings.HyperVAdminGroupWellKnownSid))
                {
                    // Dev Home contains code and error notifications to add the user to the admin group.
                    // So we do not need to send this error back to Dev Home as it will result in duplication.
                    _log.Information($"User {_windowsIdentityWrapper.UserName} not in Hyper-V admin group");
                    return new ComputeSystemsResult(new List<IComputeSystem>());
                }

                var computeSystems = _hyperVManager.GetAllVirtualMachines();
                _log.Information($"Successfully retrieved all virtual machines on: {DateTime.Now}");
                return new ComputeSystemsResult(computeSystems);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to retrieved all virtual machines on: {DateTime.Now}");
                return new ComputeSystemsResult(ex, OperationErrorString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        if (!_windowsIdentityWrapper.IsUserInGroup(HyperVStrings.HyperVAdminGroupWellKnownSid))
        {
            var hyperVAdminEx = new HyperVAdminGroupException(_stringResource.GetLocalized("UserNotInHyperVAdminGroup", _windowsIdentityWrapper.UserName));
            return new ComputeSystemAdaptiveCardResult(hyperVAdminEx, hyperVAdminEx.Message, $"User {_windowsIdentityWrapper.UserName} not in Hyper-V admin group");
        }

        var imageList = _vmGalleryService.GetGalleryImagesAsync().GetAwaiter().GetResult();
        return new ComputeSystemAdaptiveCardResult(new VMGalleryCreationAdaptiveCardSession(imageList, _stringResource));
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
            _log.Error(ex, $"Failed to create a new virtual machine on: {DateTime.Now}");

            // Dev Home will handle null values as failed operations. We can't throw because this is an out of proc
            // COM call, so we'll lose the error information. We'll log the error and return null.
            return null;
        }
    }
}
