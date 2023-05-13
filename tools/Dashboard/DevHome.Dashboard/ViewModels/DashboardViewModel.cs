// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.System;

namespace DevHome.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private const string _hideDashboardBannerKey = "HideDashboardBanner";

    [ObservableProperty]
    private bool _showDashboardBanner;

    public DashboardViewModel()
    {
        ShowDashboardBanner = ShouldShowDashboardBanner();
    }

    [RelayCommand]
    private async Task DashboardBannerButtonAsync()
    {
        await Launcher.LaunchUriAsync(new ("https://go.microsoft.com/fwlink/?linkid=2234395"));
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
        var show = true;
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        if (roamingProperties.ContainsKey(_hideDashboardBannerKey))
        {
            show = false;
        }

        return show;
    }
}
