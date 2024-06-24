// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.PI.Telemetry;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.PI.Pages;

public sealed partial class ModulesPage : Page
{
    private ModulesPageViewModel ViewModel { get; }

    public ModulesPage()
    {
        ViewModel = Application.Current.GetService<ModulesPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.LoadedModule);
    }

    private void GridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
