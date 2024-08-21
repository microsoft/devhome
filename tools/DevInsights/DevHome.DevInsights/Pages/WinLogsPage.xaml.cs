// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.DevInsights.Telemetry;
using DevHome.DevInsights.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevInsights.Pages;

public partial class WinLogsPage : Page, IDisposable
{
    private WinLogsPageViewModel ViewModel { get; }

    public WinLogsPage()
    {
        ViewModel = Application.Current.GetService<WinLogsPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.WinLogs);
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
