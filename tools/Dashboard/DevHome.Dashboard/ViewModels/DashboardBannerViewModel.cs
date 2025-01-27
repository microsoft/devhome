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
    private const string _hideDashboardBannerKey = "HideDashboardBanner";

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
#pragma warning disable CA1853
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
