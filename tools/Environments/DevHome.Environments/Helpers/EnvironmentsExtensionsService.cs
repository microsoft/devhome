// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Environments.TestModels;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Helpers;

public class EnvironmentsExtensionsService
{
    private readonly IComputeSystemManager _computeSystemManager;

    private const string EnvironmentsCreationFlowFeatureName = "EnvironmentsCreationFlow";

    private readonly IExperimentationService _experimentationService;

    public bool IsEnvironmentCreationEnabled => _experimentationService.IsFeatureEnabled(EnvironmentsCreationFlowFeatureName);

    public EnvironmentsExtensionsService(IComputeSystemManager computeSystemManager, IExperimentationService experimentationService)
    {
        _computeSystemManager = computeSystemManager;
        _experimentationService = experimentationService;
    }

    public async Task GetComputeSystemsAsync(bool useDebugValues, Func<ComputeSystemsLoadedData, Task> callback)
    {
        if (useDebugValues)
        {
            await GetTestComputeSystemsAsync(callback);
            return;
        }

        await _computeSystemManager.GetComputeSystemsAsync(callback);
    }

    // Debug only offline test data
    private async Task GetTestComputeSystemsAsync(Func<ComputeSystemsLoadedData, Task> callback)
    {
        var provider = Application.Current.GetService<IComputeSystemProvider>();

        var result = await provider.GetComputeSystemsAsync(new EmptyDevId());
        var wrapperList = new List<DeveloperIdWrapper>() { new(new EmptyDevId()) };
        if (result.Result.Status == ProviderOperationStatus.Success)
        {
            var providerWrapper = new ComputeSystemProvider(provider);
            var providerDetails = new ComputeSystemProviderDetails(new TestExtensionWrapper(), providerWrapper, wrapperList);
            var wrapperDictionary = new Dictionary<DeveloperIdWrapper, ComputeSystemsResult>() { { new DeveloperIdWrapper(new EmptyDevId()), result } };
            var loadedData = new ComputeSystemsLoadedData(providerDetails, wrapperDictionary);
            await callback(loadedData);
        }
    }
}
