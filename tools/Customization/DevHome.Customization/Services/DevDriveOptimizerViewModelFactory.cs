// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Common.Services;

/// <summary>
/// Factory class for creating <see cref="DevDriveOptimizerCardViewModel"/> instances asynchronously.
/// </summary>
public class DevDriveOptimizerViewModelFactory
{
    public Task<DevDriveOptimizerCardViewModel> CreateOptimizerCardViewModel(string cacheToBeMoved, string cacheLocation, string optimizationDescription)
    {
        var cardViewModel = new DevDriveOptimizerCardViewModel(cacheToBeMoved, cacheLocation, optimizationDescription);
        return Task.FromResult(cardViewModel);
    }
}
