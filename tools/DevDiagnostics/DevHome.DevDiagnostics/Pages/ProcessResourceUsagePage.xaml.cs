// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Telemetry;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevDiagnostics.Pages;

public sealed partial class ProcessResourceUsagePage : Page, IDisposable
{
    private ProcessResourceUsagePageViewModel ViewModel { get; }

    public ProcessResourceUsagePage()
    {
        ViewModel = Application.Current.GetService<ProcessResourceUsagePageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.ResourceUsage);
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
