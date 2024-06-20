// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.Telemetry;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.PI.Pages;

public sealed partial class WatsonsPage : Page
{
    private WatsonPageViewModel ViewModel { get; }

    public WatsonsPage()
    {
        ViewModel = Application.Current.GetService<WatsonPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Application.Current.GetService<TelemetryReporter>().SwitchTo(Feature.WERReports);
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        UpdateInfoBox();
    }

    private void WatsonsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateInfoBox();
    }

    private void UpdateInfoBox()
    {
        if (WatsonsDataGrid.SelectedItem is null)
        {
            WatsonInfo.Text = string.Empty;
            return;
        }

        SelectorBarItem selectedItem = InfoSelector.SelectedItem;
        int currentSelectedIndex = InfoSelector.Items.IndexOf(selectedItem);
        WatsonDisplayInfo info = (WatsonDisplayInfo)WatsonsDataGrid.SelectedItem;

        switch (currentSelectedIndex)
        {
            case 0: // Watson info
                WatsonInfo.Text = info.Report.Description;
                break;
            case 1: // !analyze
                WatsonInfo.Text = info.AnalyzeResults;
                break;
            case 2: // !xamltriage
                WatsonInfo.Text = "TBD";
                break;
        }
    }
}
