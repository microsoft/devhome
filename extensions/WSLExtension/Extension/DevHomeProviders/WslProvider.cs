// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.Models;

namespace WSLExtension.DevHomeProviders;

/// <summary> Provides functionality to enumerate and install WSL distributions </summary>
public class WslProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslProvider));

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public string DisplayName { get; }

    public Uri Icon { get; }

    public string Id => Constants.WslProviderId;

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public WslProvider(IStringResource stringResource, IWslManager wslManager)
    {
        _stringResource = stringResource;
        _wslManager = wslManager;
        DisplayName = _stringResource.GetLocalized("ProviderDisplayName");
        Icon = new(Constants.ExtensionIcon);
    }

    /// <summary>
    /// Creates and returns the adaptive card session that will appear in the create environment creation UX in Dev Home.
    /// </summary>
    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        var distributionList = _wslManager.GetAllDistributionsAvailableToInstallAsync().GetAwaiter().GetResult();
        return new ComputeSystemAdaptiveCardResult(new WslAvailableDistrosAdaptiveCardSession(distributionList, _stringResource));
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

            var distributionList = _wslManager.GetAllDistributionsAvailableToInstallAsync().GetAwaiter().GetResult();
            return new WslInstallAndRegisterDistributionOperation(
                distributionList[wslInstallationUserInput.SelectedDistributionIndex],
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
                _log.Error($"Failed to retrieve all wsl distributions", ex);
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
}
