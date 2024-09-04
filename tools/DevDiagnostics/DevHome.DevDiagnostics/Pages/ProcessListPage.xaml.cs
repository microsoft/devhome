// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Telemetry;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevDiagnostics.Pages;

public sealed partial class ProcessListPage : Page
{
    private ProcessListPageViewModel ViewModel { get; }

    public ProcessListPage()
    {
        ViewModel = Application.Current.GetService<ProcessListPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.ResetPage();
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.ProcessList);
    }
}
