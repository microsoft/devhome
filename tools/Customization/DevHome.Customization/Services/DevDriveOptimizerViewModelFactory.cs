// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Common.Services;

/// <summary>
/// Factory class for creating <see cref="DevDriveOptimizerCardViewModel"/> instances asynchronously.
/// </summary>
public class DevDriveOptimizerViewModelFactory
{
    public Task<DevDriveOptimizerCardViewModel> CreateOptimizerCardViewModel(string cacheToBeMoved, string existingCacheLocation, string exampleLocationOnDevDrive, string environmentVariableToBeSet)
    {
        var cardViewModel = new DevDriveOptimizerCardViewModel(cacheToBeMoved, existingCacheLocation, exampleLocationOnDevDrive, environmentVariableToBeSet);
        return Task.FromResult(cardViewModel);
    }
}
