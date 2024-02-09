// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Environments.TestModels;
using DevHome.Environments.ViewModels;
using DevHome.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Helpers;

public class EnvironmentsExtensionsService
{
    private readonly IComputeSystemService _computeSystemService;

    private readonly IAccountsService _accountService;

    private Dictionary<IComputeSystemProvider, List<IDeveloperId>>? _providers;

    public EnvironmentsExtensionsService(
        IComputeSystemService computeSystemService,
        IAccountsService accountService)
    {
        _computeSystemService = computeSystemService;
        _accountService = accountService;
    }

    public async Task<Collection<ComputeSystemViewModel>> GetComputeSystemsAsync(bool useDebugValues)
    {
        if (useDebugValues)
        {
            return await GetTestComputeSystemsAsync();
        }

        _providers ??= await _computeSystemService.GetComputeSystemProvidersAsync();

        var computeSystems = new Collection<ComputeSystemViewModel>();

        foreach (var providerAndDevIds in _providers)
        {
            ComputeSystemsResult? result = null;
            var provider = providerAndDevIds.Key;
            var devIdList = providerAndDevIds.Value;

            if (devIdList.Count == 0)
            {
                result = await provider.GetComputeSystemsAsync(new EmptyDevId(), string.Empty);
            }
            else
            {
                foreach (var devId in devIdList)
                {
                    result = await provider.GetComputeSystemsAsync(devId, string.Empty);
                }
            }

            if (result is not null && result.Result.Status != ProviderOperationStatus.Success)
            {
                GlobalLog.Logger?.ReportError($"Failed to get {nameof(IComputeSystemProvider)} provider from '{provider.DisplayName}'", result.Result.ToString() ?? string.Empty);
                continue;
            }

            foreach (var system in result?.ComputeSystems ?? Enumerable.Empty<IComputeSystem>())
            {
                computeSystems.Add(new ComputeSystemViewModel(system, provider.DisplayName));
            }
        }

        return computeSystems;
    }

    // Debug only offline test data
    private async Task<Collection<ComputeSystemViewModel>> GetTestComputeSystemsAsync()
    {
        var computeSystems = new Collection<ComputeSystemViewModel>();
        var provider = Application.Current.GetService<IComputeSystemProvider>();

        var result = await provider.GetComputeSystemsAsync(new EmptyDevId(), string.Empty);
        if (result.Result.Status == ProviderOperationStatus.Success)
        {
            foreach (var system in result?.ComputeSystems ?? Enumerable.Empty<IComputeSystem>())
            {
                computeSystems.Add(new ComputeSystemViewModel(system, provider.DisplayName));
            }
        }

        return computeSystems;
    }
}
