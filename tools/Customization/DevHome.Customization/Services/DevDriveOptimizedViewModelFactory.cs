// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Common.Services;

/// <summary>
/// Factory class for creating <see cref="DevDriveOptimizedCardViewModel"/> instances asynchronously.
/// </summary>
public class DevDriveOptimizedViewModelFactory
{
    public Task<DevDriveOptimizedCardViewModel> CreateOptimizedCardViewModel(string cacheMoved, string optimizedCacheLocation, string environmentVariableSet)
    {
        var cardViewModel = new DevDriveOptimizedCardViewModel(cacheMoved, optimizedCacheLocation, environmentVariableSet);
        return Task.FromResult(cardViewModel);
    }
}
