// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Common.Services;

/// <summary>
/// Factory class for creating <see cref="DevDriveCardViewModel"/> instances asynchronously.
/// </summary>
public class DevDriveViewModelFactory
{
    public Task<DevDriveCardViewModel> CreateCardViewModel(IDevDriveManager manager, IDevDrive devDrive/*, DevDriveProvider provider, string devDrivelabel*/)
    {
        var cardViewModel = new DevDriveCardViewModel(devDrive, manager);
        return Task.FromResult(cardViewModel);
    }
}
