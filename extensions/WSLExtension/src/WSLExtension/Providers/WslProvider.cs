// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Common;
using WSLExtension.Models;
using WSLExtension.Services;

namespace WSLExtension.Providers;

/// <summary> Class that provides information for WSL installed distributions. </summary>
public class WslProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslProvider));

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public WslProvider(IStringResource stringResource, IWslManager wslManager)
    {
        _stringResource = stringResource;
        _wslManager = wslManager;
    }

    public string OperationErrorString => "ErrorPerformingOperation";

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        var task = _wslManager.GetOnlineAvailableDistributions();
        task.Wait();

        var distroList = task.Result;

        return new ComputeSystemAdaptiveCardResult(new WslAvailableDistrosAdaptiveCardSession(distroList, _stringResource));
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) =>
        throw new NotImplementedException();

    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId? developerId, string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, typeof(WslInstallationUserInput));
            var wslInstallationUserInput = deserializedObject as WslInstallationUserInput ?? throw new InvalidOperationException($"Json deserialization failed for input Json: {inputJson}");

            var task = _wslManager.GetOnlineAvailableDistributions();
            task.Wait();

            var distroList = task.Result;

            return new WslInstallAndRegisterDistroOperation(
                distroList[wslInstallationUserInput.SelectedDistroListIndex],
                _stringResource,
                _wslManager);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to install WSL distro on: {DateTime.Now}");

            // Dev Home will handle null values as failed operations. We can't throw because this is an out of proc
            // COM call, so we'll lose the error information. We'll log the error and return null.
            return null;
        }
    }

    public string DisplayName => Constants.WslProviderDisplayName;

    public Uri Icon => new(Constants.ExtensionIcon);

    public string Id => Constants.WslProviderId;

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            try
            {
                var computeSystems = _wslManager.GetAllRegisteredDistributions();

                _log.Information($"Successfully retrieved all wsl distros on: {DateTime.Now}");
                return new ComputeSystemsResult(computeSystems);
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to retrieve all wsl distros on: {DateTime.Now}", ex);
                return new ComputeSystemsResult(ex, OperationErrorString, ex.Message);
            }
        }).AsAsyncOperation();
    }
}
