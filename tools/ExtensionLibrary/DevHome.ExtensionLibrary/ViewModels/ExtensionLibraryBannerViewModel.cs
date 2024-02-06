// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.System;

namespace DevHome.ExtensionLibrary.ViewModels;

internal sealed partial class ExtensionLibraryBannerViewModel : ObservableObject
{
    private const string _hideExtensionsBannerKey = "HideExtensionsBanner";

    [ObservableProperty]
    private bool _showExtensionsBanner;

    public ExtensionLibraryBannerViewModel()
    {
        ShowExtensionsBanner = ShouldShowExtensionsBanner();
    }

    [RelayCommand]
    private async Task ExtensionsBannerButtonAsync()
    {
        await Launcher.LaunchUriAsync(new("https://go.microsoft.com/fwlink/?linkid=2247301"));
    }

    [RelayCommand]
    private void HideExtensionsBannerButton()
    {
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        roamingProperties[_hideExtensionsBannerKey] = bool.TrueString;
        ShowExtensionsBanner = false;
    }

    private bool ShouldShowExtensionsBanner()
    {
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        return !roamingProperties.ContainsKey(_hideExtensionsBannerKey);
    }
}
