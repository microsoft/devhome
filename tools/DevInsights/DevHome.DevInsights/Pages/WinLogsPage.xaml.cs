// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.DevInsights.Models;
using DevHome.DevInsights.Telemetry;
using DevHome.DevInsights.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevInsights.Pages;

public partial class WinLogsPage : Page, IDisposable
{
    public WinLogsPageViewModel ViewModel { get; }

    public WinLogsPage()
    {
        ViewModel = Application.Current.GetService<WinLogsPageViewModel>();
        InitializeComponent();
        DataContext = this;
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
