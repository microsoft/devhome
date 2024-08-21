// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevInsights.Telemetry;
using DevHome.DevInsights.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevInsights.Pages;

public partial class AppDetailsPage : Page
{
    private AppDetailsPageViewModel ViewModel { get; }

    public AppDetailsPage()
    {
        ViewModel = Application.Current.GetService<AppDetailsPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.AppDetails);
    }
}
