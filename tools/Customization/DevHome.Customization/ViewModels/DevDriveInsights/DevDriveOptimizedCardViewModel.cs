// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;

namespace DevHome.Customization.ViewModels.DevDriveInsights;

/// <summary>
/// View model for the card that represents an optimized cache on the dev drive insights page.
/// </summary>
public partial class DevDriveOptimizedCardViewModel : ObservableObject
{
    public string CacheMoved { get; set; }

    public string OptimizedCacheLocation { get; set; }

    public string EnvironmentVariableSet { get; set; }

    public string OptimizedDevDriveDescription { get; set; }

    public string DevDriveOptimized { get; set; }

    public DevDriveOptimizedCardViewModel(string cacheMoved, string optimizedCacheLocation, string environmentVariableSet)
    {
        CacheMoved = cacheMoved;
        OptimizedCacheLocation = optimizedCacheLocation;
        EnvironmentVariableSet = environmentVariableSet;
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        OptimizedDevDriveDescription = stringResource.GetLocalized("OptimizedDevDriveDescription", EnvironmentVariableSet, OptimizedCacheLocation);
        DevDriveOptimized = stringResource.GetLocalized("DevDriveOptimized");
    }
}
