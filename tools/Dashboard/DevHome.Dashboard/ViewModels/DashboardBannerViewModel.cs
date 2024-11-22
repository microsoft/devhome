// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.System;

namespace DevHome.Dashboard.ViewModels;

internal sealed partial class DashboardBannerViewModel : ObservableObject
{
    #pragma warning disable IDE1006 // Naming Styles
    private const string _hideDashboardBannerKey = "HideDashboardBanner";
    #pragma warning restore IDE1006 // Naming Styles

    [ObservableProperty]
    private bool _showDashboardBanner;

    public DashboardBannerViewModel()
    {
        ShowDashboardBanner = ShouldShowDashboardBanner();
    }

    [RelayCommand]
    private async Task DashboardBannerButtonAsync()
    {
        await Launcher.LaunchUriAsync(new("https://go.microsoft.com/fwlink/?linkid=2234395"));
    }

    [RelayCommand]
    private void HideDashboardBannerButton()
    {
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        roamingProperties[_hideDashboardBannerKey] = bool.TrueString;
        ShowDashboardBanner = false;
    }

    private bool ShouldShowDashboardBanner()
    {
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        return !roamingProperties.ContainsKey(_hideDashboardBannerKey);
    }

#if DEBUG
    public void ResetDashboardBanner()
    {
        ShowDashboardBanner = true;
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        if (roamingProperties.ContainsKey(_hideDashboardBannerKey))
        {
            roamingProperties.Remove(_hideDashboardBannerKey);
        }
    }
#endif
}
