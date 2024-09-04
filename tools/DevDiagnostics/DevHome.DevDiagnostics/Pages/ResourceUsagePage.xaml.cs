// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Telemetry;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevDiagnostics.Pages;

public sealed partial class ResourceUsagePage : Page
{
    private ResourceUsagePageViewModel ViewModel { get; }

    public ResourceUsagePage()
    {
        ViewModel = Application.Current.GetService<ResourceUsagePageViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.ResourceUsage);
        Application.Current.GetService<SystemResourceUsagePageViewModel>().Start();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        Application.Current.GetService<SystemResourceUsagePageViewModel>().Stop();
    }
}
