// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public IWidgetHostingService WidgetHostingService { get; }

    public IWidgetIconService WidgetIconService { get; }

    private readonly IPackageDeploymentService _packageDeploymentService;

    private bool _validatedWebExpPack;

    [ObservableProperty]
    private bool _dashboardNeedsRestart;

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
    }

    public bool EnsureWebExperiencePack()
    {
        // If already validated there's a good version, don't check again.
        if (_validatedWebExpPack)
        {
            return true;
        }

        var minSupportedVersion400 = new Version(423, 3800);
        var minSupportedVersion500 = new Version(523, 3300);
        var version500 = new Version(500, 0);

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
        if (widgetCount == 0 && !isLoading && !DashboardNeedsRestart)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }
}
