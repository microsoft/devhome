// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.Telemetry;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.PI.Pages;

public partial class WinLogsPage : Page, IDisposable
{
    private WinLogsPageViewModel ViewModel { get; }

    public ObservableCollection<WinLogCategory> WinLogCategories { get; }

    public WinLogsPage()
    {
        WinLogCategories = new ObservableCollection<WinLogCategory>(Enum.GetValues(typeof(WinLogCategory)).Cast<WinLogCategory>());
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
