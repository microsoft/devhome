// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.ViewModels.Environments;
using Serilog;
using WinUIEx;

namespace DevHome.Common.Services;

/// <summary>
/// Factory class for creating <see cref="ComputeSystemCardViewModel"/> instances asynchronously.
/// </summary>
public class ComputeSystemViewModelFactory
{
    public async Task<ComputeSystemCardViewModel> CreateCardViewModelAsync(
        IComputeSystemManager manager,
        ComputeSystemCache computeSystem,
        ComputeSystemProvider provider,
        string packageFullName,
        WindowEx windowEx)
    {
        var cardViewModel = new ComputeSystemCardViewModel(computeSystem, manager, windowEx);

        try
        {
            cardViewModel.CardState = await cardViewModel.GetCardStateAsync();
            cardViewModel.ComputeSystemImage = await ComputeSystemHelpers.GetBitmapImageAsync(computeSystem);
            cardViewModel.ComputeSystemProviderName = provider.DisplayName;
            cardViewModel.ComputeSystemProviderImage = CardProperty.ConvertMsResourceToIcon(provider.Icon, packageFullName);
            cardViewModel.ComputeSystemProperties = await ComputeSystemHelpers.GetComputeSystemCardPropertiesAsync(computeSystem, packageFullName);
        }
        catch (Exception ex)
        {
            var log = Log.ForContext("SourceContext", nameof(ComputeSystemViewModelFactory));
            log.Error(ex, $"Failed to get initial properties for compute system {computeSystem}. Error: {ex.Message}");
        }

        return cardViewModel;
    }
}
