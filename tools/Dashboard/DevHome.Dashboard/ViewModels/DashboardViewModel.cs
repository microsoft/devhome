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

    private bool _showDashboardBanner;

    public bool ShowDashboardBanner
    {
        get => _showDashboardBanner;
        set => SetProperty(ref _showDashboardBanner, value);
    }

    public DashboardViewModel()
    {
        ShowDashboardBanner = ShouldShowDashboardBanner();
    }

    [RelayCommand]
    private async Task DashboardBannerButtonAsync()
    {
        // TODO Update code with the "Learn more" button behavior
        await Launcher.LaunchUriAsync(new ("https://microsoft.com"));
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
