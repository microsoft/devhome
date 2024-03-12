// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels.Environments;

namespace DevHome.Common.Services;

/// <summary>
/// Factory class for creating <see cref="DevDriveCardViewModel"/> instances asynchronously.
/// </summary>
public class DevDriveViewModelFactory
{
    public Task<DevDriveCardViewModel> CreateCardViewModel(IDevDriveManager manager, IDevDrive devDrive/*, DevDriveProvider provider, string devDrivelabel*/)
    {
        var cardViewModel = new DevDriveCardViewModel(devDrive, manager);

        try
        {
            // cardViewModel.CardState = await cardViewModel.GetCardStateAsync();
            // cardViewModel.DevDriveImage = await DevDriveHelpers.GetBitmapImageAsync(devDrive);
            // cardViewModel.DevDriveProviderName = provider.DisplayName;
            // cardViewModel.DevDriveProviderImage = CardProperty.ConvertMsResourceToIcon(provider.Icon, packageFullName);
            // cardViewModel.DevDriveProperties = await DevDriveHelpers.GetDevDrivePropertiesAsync(devDrive, devDrivelabel);
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger.ReportError(Log.Component.DevDriveViewModelFactory, $"Failed to get initial properties for dev drive {devDrive}. Error: {ex.Message}");
        }

        return Task.FromResult(cardViewModel);
    }
}
