// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;

using Dispatching = Microsoft.UI.Dispatching;

namespace DevHome.Customization.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a dev drive on the setup target page.
/// </summary>
public partial class DevDriveOptimizedCardViewModel : ObservableObject
{
    public string CacheMoved { get; set; }

    public string OptimizedCacheLocation { get; set; }

    public string EnvironmentVariableSet { get; set; }

    public DevDriveOptimizedCardViewModel(string cacheMoved, string optimizedCacheLocation, string environmentVariableSet)
    {
        CacheMoved = cacheMoved;
        OptimizedCacheLocation = optimizedCacheLocation;
        EnvironmentVariableSet = environmentVariableSet;
    }
}
