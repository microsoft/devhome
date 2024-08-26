// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevInsights.Controls;
using DevHome.DevInsights.Telemetry;
using DevHome.DevInsights.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevInsights.Pages;

public sealed partial class InsightsPage : Page
{
    private InsightsPageViewModel ViewModel { get; }

    public InsightsPage()
    {
        ViewModel = Application.Current.GetService<InsightsPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.Insights);
    }
}
