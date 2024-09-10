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

        // Populate selector items for each WER analyizer registered with the system
        foreach (Tool tool in ViewModel.RegisteredAnalysisTools)
        {
            InfoSelector.Items.Add(CreateSelectorBarItemForDebugAnalyzer(tool));
        }

        ((INotifyCollectionChanged)ViewModel.RegisteredAnalysisTools).CollectionChanged += RegisteredAnalysisTools_CollectionChanged;
    }

    private void RegisteredAnalysisTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // If we have a new tool, add a new selector item for it
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (Tool tool in e.NewItems)
            {
                InfoSelector.Items.Add(CreateSelectorBarItemForDebugAnalyzer(tool));
            }
        }

        // Or if we removed a tool, remove the selector item for it
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (Tool tool in e.OldItems)
            {
                foreach (var item in InfoSelector.Items.Where(x => x.Tag == tool))
                {
                    InfoSelector.Items.Remove(item);
                    break;
                }
            }
        }
    }

    private SelectorBarItem CreateSelectorBarItemForDebugAnalyzer(Tool tool)
    {
        SelectorBarItem selectorBarItem = new()
        {
            Text = tool.Name,
            Tag = tool,
        };

        MenuFlyout menuFlyout = new();
        MenuFlyoutItem item = new()
        {
            Text = _bucketUsingThisToolString,
            Tag = tool,
        };
        item.Click += (sender, e) =>
        {
            ViewModel.SetBucketingTool(tool);
        };
        menuFlyout.Items.Add(item);

        selectorBarItem.ContextFlyout = menuFlyout;
        return selectorBarItem;
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
        WERAnalysisReport info = (WERAnalysisReport)WERDataGrid.SelectedItem;

        if (selectedItem is not null && selectedItem.Tag is Tool tool)
        {
            if (info.ToolAnalyses.TryGetValue(tool, out var analysis))
            {
                WERInfo.Text = analysis.Analysis;
            }
            else
            {
               WERInfo.Text = CommonHelper.GetLocalizedString("WERAnalysisUnavailable");
            }
        }
        else
        {
            Debug.Assert(currentSelectedIndex == 0 || currentSelectedIndex == -1, "Expected only the first item would have a null tag");
            WERInfo.Text = info.Report.Description;
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

        WERAnalysisReport? info = hyperlinkButton.Tag as WERAnalysisReport;
        Debug.Assert(info is not null, "This object should have a Tag with a WERDisplayInfo");

        ViewModel.OpenCab(info.Report.CrashDumpPath);
    }
}
