// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.Telemetry;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.PI.Pages;

public sealed partial class WERPage : Page
{
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

        SelectorBarItem selectedItem = InfoSelector.SelectedItem;
        int currentSelectedIndex = InfoSelector.Items.IndexOf(selectedItem);
        WERDisplayInfo info = (WERDisplayInfo)WERDataGrid.SelectedItem;

        switch (currentSelectedIndex)
        {
            case 0: // WER info
                WERInfo.Text = info.Report.Description;
                break;
        }
    }

    private void WERDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
        if (e.Column.Tag is not null)
        {
            bool sortAscending = e.Column.SortDirection == DataGridSortDirection.Ascending;

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
            else if (tag == "WERBucket")
            {
                ViewModel.SortByWERBucket(sortAscending);
            }
            else if (tag == "CrashDumpPath")
            {
                ViewModel.SortByCrashDumpPath(sortAscending);
            }

            e.Column.SortDirection = sortAscending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;

            // Clear the sort direction for the other columns
            foreach (DataGridColumn column in WERDataGrid.Columns)
            {
                if (column != e.Column)
                {
                    column.SortDirection = null;
                }
            }
        }
    }

    private void LocalDumpCollection_Toggled(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeLocalCollectionForApp(LocalDumpCollectionToggle.IsOn);
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        HyperlinkButton? hyperlinkButton = sender as HyperlinkButton;
        Debug.Assert(hyperlinkButton is not null, "Who called HyperlinkButton_Click that wasn't a hyperlink button?");

        WERDisplayInfo? info = hyperlinkButton.Tag as WERDisplayInfo;
        Debug.Assert(info is not null, "This object should have a Tag with a WERDisplayInfo");

        ViewModel.OpenCab(info.Report.CrashDumpPath);
    }
}
