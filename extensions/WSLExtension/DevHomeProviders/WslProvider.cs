// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management;
using System.Text.Json;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.Models;
using static WSLExtension.Constants;

namespace WSLExtension.DevHomeProviders;

/// <summary> Provides functionality to enumerate and install WSL distributions </summary>
public class WslProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslProvider));

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    private readonly string _wslEnablementError;

    private const uint FeatureEnabledState = 1;

    public string DisplayName => WslProviderDisplayName;

    public Uri Icon { get; }

    public string Id => WslProviderId;

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public WslProvider(IStringResource stringResource, IWslManager wslManager)
    {
        _stringResource = stringResource;
        _wslManager = wslManager;
        Icon = new(ExtensionIcon);
        _wslEnablementError = GetErrorStringForWhenFeatureNotEnabled();
    }

    /// <summary>
    /// Creates and returns the adaptive card session that will appear in the create environment creation UX in Dev Home.
    /// </summary>
    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        if (!string.IsNullOrEmpty(_wslEnablementError))
        {
            _log.Error($"Virtual machine platform optional component not enabled. A reboot is needed.");
            return new ComputeSystemAdaptiveCardResult(new InvalidOperationException(), _wslEnablementError, _wslEnablementError);
        }

        var definitions = _wslManager.GetAllDistributionsAvailableToInstallAsync().GetAwaiter().GetResult();
        return new ComputeSystemAdaptiveCardResult(new RegisterAndInstallDistributionSession(definitions, _stringResource));
    }

    /// <summary>
    /// Creates the operation that when started will install and register the WSL distribution.
    /// </summary>
    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId? developerId, string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, typeof(WslInstallationUserInput));
            var wslInstallationUserInput =
                deserializedObject as WslInstallationUserInput ?? throw new InvalidOperationException($"Json deserialization failed for input Json: {inputJson}");

            var definitions = _wslManager.GetAllDistributionsAvailableToInstallAsync().GetAwaiter().GetResult();
            return new WslInstallDistributionOperation(
                definitions[wslInstallationUserInput.SelectedDistributionIndex],
                _stringResource,
                _wslManager);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to create the compute system creation operation. InputJson: {inputJson}");

            // Dev Home will handle null values as failed operations. We can't throw because this is an out of proc
            // COM call, so we'll lose the error information. We'll log the error and return null.
            return null;
        }
    }

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(async () =>
        {
            try
            {
                var computeSystems = await _wslManager.GetAllRegisteredDistributionsAsync();

                _log.Information($"Successfully retrieved all wsl distributions");
                return new ComputeSystemsResult(computeSystems);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to retrieve all wsl distributions");
                return new ComputeSystemsResult(ex, ex.Message, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(
        IComputeSystem computeSystem,
        ComputeSystemAdaptiveCardKind sessionKind)
    {
        var notImplementedException = new NotImplementedException($"Method not implemented by WSL Compute System Provider");
        return new ComputeSystemAdaptiveCardResult(notImplementedException, notImplementedException.Message, notImplementedException.Message);
    }

    /// <summary>
    /// Gets the localized error message for when the virtual machine platform Windows optional component
    /// is not enabled.
    /// </summary>
    /// <returns>
    /// Returns the error message for when the feature is not enabled and returns an empty string when the feature is enabled.
    /// </returns>
    private string GetErrorStringForWhenFeatureNotEnabled()
    {
        var searcher = new ManagementObjectSearcher($"SELECT InstallState FROM Win32_OptionalFeature WHERE Name = 'VirtualMachinePlatform'");
        var collection = searcher.Get();

        foreach (var instance in collection)
        {
            var enablementState = instance?.GetPropertyValue("InstallState") as uint?;
            _log.Information($"Found VirtualMachinePlatform feature with enablement state: '{enablementState}'");

            if (enablementState != FeatureEnabledState)
            {
                return _stringResource.GetLocalized("VirtualMachinePlatformNotInstalled");
            }
        }

        // VirtualMachinePlatform feature enabled, no error message to send.
        return string.Empty;
    }
}
