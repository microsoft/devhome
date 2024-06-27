// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class WatsonPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private ObservableCollection<WatsonDisplayInfo> _displayedReports = [];

    [ObservableProperty]
    private string _watsonInfoText;

    [ObservableProperty]
    private bool _applyFilter = true;

    public WatsonPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _watsonInfoText = string.Empty;

        ((INotifyCollectionChanged)WatsonHelper.Instance.WatsonReports).CollectionChanged += WatsonOutput_CollectionChanged;

        PopulateCurrentLogs();
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            PopulateCurrentLogs();
        }
    }

    private void WatsonOutput_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            _dispatcher.TryEnqueue(() =>
            {
                FilterWatsonReportList(e.NewItems);
            });
        }
    }

    partial void OnApplyFilterChanged(bool value)
    {
        PopulateCurrentLogs();
    }

    private void PopulateCurrentLogs()
    {
        _dispatcher.TryEnqueue(() =>
        {
            DisplayedReports.Clear();

            FilterWatsonReportList(WatsonHelper.Instance.WatsonReports.ToList<WatsonReport>());
        });
    }

    private void FilterWatsonReportList(System.Collections.IList? reportList)
    {
        // Get all existing reports
        foreach (var report in WatsonHelper.Instance.WatsonReports)
        {
            // Provide filtering if needed
            if (!ApplyFilter ||
                (TargetAppData.Instance.TargetProcess is not null &&
                report.FilePath.Contains(TargetAppData.Instance.TargetProcess.ProcessName, StringComparison.OrdinalIgnoreCase)))
            {
                DisplayedReports.Add(new WatsonDisplayInfo(report));
            }
        }
    }

    internal void SortByFaultingExecutable(bool sortAscending)
    {
        ObservableCollection<WatsonDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderBy(x => x.Report.Executable));
        }
        else
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderByDescending(x => x.Report.Executable));
        }

        DisplayedReports = sortedCollection;
    }

    internal void SortByDateTime(bool sortAscending)
    {
        ObservableCollection<WatsonDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderBy(x => x.Report.TimeGenerated));
        }
        else
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderByDescending(x => x.Report.TimeGenerated));
        }

        DisplayedReports = sortedCollection;
    }

    internal void SortByWatsonBucket(bool sortAscending)
    {
        ObservableCollection<WatsonDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderBy(x => x.FailureBucket));
        }
        else
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderByDescending(x => x.FailureBucket));
        }

        DisplayedReports = sortedCollection;
    }

    internal void SortByCrashDumpPath(bool sortAscending)
    {
        ObservableCollection<WatsonDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderBy(x => x.Report.CrashDumpPath));
        }
        else
        {
            sortedCollection = new ObservableCollection<WatsonDisplayInfo>(DisplayedReports.OrderByDescending(x => x.Report.CrashDumpPath));
        }

        DisplayedReports = sortedCollection;
    }
}
