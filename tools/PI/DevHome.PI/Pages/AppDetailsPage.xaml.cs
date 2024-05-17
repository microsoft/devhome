// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.PI.Telemetry;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.PI.Pages;

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
