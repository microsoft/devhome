// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
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

    private void WatsonsDataGrid_Sorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
    {
        if (e.Column.Tag is not null)
        {
            bool sortAscending = e.Column.SortDirection == CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Ascending;

            // Flip the sort direction
            sortAscending = !sortAscending;

            string? tag = e.Column.Tag.ToString();
            Debug.Assert(tag is not null, "Why is the tag null?");

            if (tag == "DateTime")
            {
                ViewModel.SortByDateTime(sortAscending);
            }
            else if (tag == "FaultingExecutable")
            {
                ViewModel.SortByFaultingExecutable(sortAscending);
            }
            else if (tag == "WatsonBucket")
            {
                ViewModel.SortByWatsonBucket(sortAscending);
            }
            else if (tag == "CrashDumpPath")
            {
                ViewModel.SortByCrashDumpPath(sortAscending);
            }

            e.Column.SortDirection = sortAscending ? CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Ascending : CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Descending;

            // Clear the sort direction for the other columns
            foreach (CommunityToolkit.WinUI.UI.Controls.DataGridColumn column in WatsonsDataGrid.Columns)
            {
                if (column != e.Column)
                {
                    column.SortDirection = null;
                }
            }
        }
    }

    private void LocalWatsonCollection_Toggled(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeLocalCollectionForApp(LocalWatsonCollectionToggle.IsOn);
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        HyperlinkButton? hyperlinkButton = sender as HyperlinkButton;
        Debug.Assert(hyperlinkButton is not null, "Who called HyperlinkButton_Click that wasn't a hyperlink button?");

        WatsonDisplayInfo? info = hyperlinkButton.Tag as WatsonDisplayInfo;
        Debug.Assert(info is not null, "This object should have a Tag with a WatsonDisplayInfo");

        ViewModel.OpenCab(info.Report.CrashDumpPath);
    }
}
