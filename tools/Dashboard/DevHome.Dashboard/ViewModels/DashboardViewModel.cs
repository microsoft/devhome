// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.System;

namespace DevHome.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public IWidgetHostingService WidgetHostingService { get; }

    public IWidgetIconService WidgetIconService { get; }

    private readonly IPackageDeploymentService _packageDeploymentService;

    private readonly Version minSupportedVersion400 = new (423, 3800);
    private readonly Version minSupportedVersion500 = new (523, 3300);
    private readonly Version version500 = new (500, 0);

    private bool _validatedWebExpPack;

    // Banner properties
    private const string _hideDashboardBannerKey = "HideDashboardBanner";

    [ObservableProperty]
    private bool _showDashboardBanner;

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel(
        IPackageDeploymentService packageDeploymentService,
        IWidgetHostingService widgetHostingService,
        IWidgetIconService widgetIconService)
    {
        _packageDeploymentService = packageDeploymentService;
        WidgetIconService = widgetIconService;

        WidgetHostingService = widgetHostingService;

        ShowDashboardBanner = ShouldShowDashboardBanner();
    }

    public bool EnsureWebExperiencePack()
    {
        // If already validated there's a good version, don't check again.
        if (_validatedWebExpPack)
        {
            return true;
        }

        // Ensure the application is installed, and the version is high enough.
        const string packageFamilyName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(
            packageFamilyName,
            (minSupportedVersion400, version500),
            (minSupportedVersion500, null));
        _validatedWebExpPack = packages.Any();
        return _validatedWebExpPack;
    }

    public Visibility GetNoWidgetMessageVisibility(int widgetCount, bool isLoading)
    {
        if (widgetCount == 0 && !isLoading)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    // =============================================================================================
    // Banner methods
    // =============================================================================================
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
