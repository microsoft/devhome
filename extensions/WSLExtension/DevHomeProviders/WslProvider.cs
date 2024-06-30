// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.Models;

namespace WSLExtension.DevHomeProviders;

/// <summary> Class that provides information for WSL installed distributions. </summary>
public class WslProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslProvider));

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public string DisplayName => Constants.WslProviderDisplayName;

    public Uri Icon => new(Constants.ExtensionIcon);

    public string Id => Constants.WslProviderId;

    public string OperationErrorString => "ErrorPerformingOperation";

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public WslProvider(IStringResource stringResource, IWslManager wslManager)
    {
        _stringResource = stringResource;
        _wslManager = wslManager;
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        var distributionList = _wslManager.GetAllDistributionInfoFromGitHubAsync().GetAwaiter().GetResult();
        return new ComputeSystemAdaptiveCardResult(new WslAvailableDistrosAdaptiveCardSession(distributionList, _stringResource));
    }

    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId? developerId, string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, typeof(WslInstallationUserInput));
            var wslInstallationUserInput =
                deserializedObject as WslInstallationUserInput ?? throw new InvalidOperationException($"Json deserialization failed for input Json: {inputJson}");

            var distributionList = _wslManager.GetAllDistributionInfoFromGitHubAsync().GetAwaiter().GetResult();

            if (wslInstallationUserInput.SelectedDistroListIndex < 0 ||
                wslInstallationUserInput.SelectedDistroListIndex > distributionList.Count)
            {
                throw new InvalidOperationException($"Provided index {wslInstallationUserInput.SelectedDistroListIndex} invalid. " +
                    $"Available number of distributions: {distributionList.Count}");
            }

            return new WslInstallAndRegisterDistroOperation(
                distributionList[wslInstallationUserInput.SelectedDistroListIndex],
                _stringResource,
                _wslManager);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to install WSL distro");

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

                _log.Information($"Successfully retrieved all wsl distros");
                return new ComputeSystemsResult(computeSystems);
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to retrieve all wsl distros", ex);
                return new ComputeSystemsResult(ex, OperationErrorString, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) =>
        throw new NotImplementedException();
}
