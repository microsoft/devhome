// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Telemetry;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevDiagnostics.Pages;

public sealed partial class WERPage : Page
{
    private readonly string _bucketUsingThisToolString = CommonHelper.GetLocalizedString("BucketUsingThisToolString");

    private WERPageViewModel ViewModel { get; }

    public WERPage()
    {
        ViewModel = Application.Current.GetService<WERPageViewModel>();
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

    private void WERDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateInfoBox();
    }

    private void UpdateInfoBox()
    {
        if (WERDataGrid.SelectedItem is null)
        {
            WERInfo.Text = string.Empty;
            return;
        }
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        HyperlinkButton? hyperlinkButton = sender as HyperlinkButton;
        Debug.Assert(hyperlinkButton is not null, "Who called HyperlinkButton_Click that wasn't a hyperlink button?");

        WERReport? info = hyperlinkButton.Tag as WERReport;
        Debug.Assert(info is not null, "This object should have a Tag with a WERDisplayInfo");

        ViewModel.OpenCab(info.BasicReport.CrashDumpPath);
    }
}
