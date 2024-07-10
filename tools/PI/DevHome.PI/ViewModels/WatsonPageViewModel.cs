// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;

namespace DevHome.PI.ViewModels;

public partial class WatsonPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private ObservableCollection<WatsonDisplayInfo> _displayedReports = [];

    [ObservableProperty]
    private string _watsonInfoText;

    private bool _applyFilter = true;

    [ObservableProperty]
    private bool _attachedToApp;

    [ObservableProperty]
    private bool _localCollectionEnabledForApp;

    [ObservableProperty]
    private bool _runningAsAdmin;

    [ObservableProperty]
    private bool _allowElevationOption;

    private delegate int WatsonCompareFunction(WatsonDisplayInfo info1, WatsonDisplayInfo info2, bool sortAscending);

    private WatsonCompareFunction? _currentCompareFunction;
    private bool? _currentSortAscending;

    public WatsonPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _watsonInfoText = string.Empty;
        _applyFilter = Settings.Default.ApplyAppFilteringToData;
        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        RunningAsAdmin = RuntimeHelper.IsCurrentProcessRunningAsAdmin();
        AllowElevationOption = !RunningAsAdmin;

        string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;
        AttachedToApp = attachedApp is not null;
        LocalCollectionEnabledForApp = attachedApp is not null ? WatsonHelper.Instance.IsCollectionEnabledForApp(attachedApp + ".exe") : false;

        ((INotifyCollectionChanged)WatsonHelper.Instance.WatsonReports).CollectionChanged += WatsonOutput_CollectionChanged;

        PopulateCurrentLogs();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.ApplyAppFilteringToData))
        {
            _applyFilter = Settings.Default.ApplyAppFilteringToData;
            PopulateCurrentLogs();
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            PopulateCurrentLogs();

            string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;
            AttachedToApp = attachedApp is not null;
            LocalCollectionEnabledForApp = attachedApp is not null ? WatsonHelper.Instance.IsCollectionEnabledForApp(attachedApp + ".exe") : false;
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
        if (reportList is null)
        {
            return;
        }

        // Get all existing reports
        foreach (WatsonReport report in reportList)
        {
            // Provide filtering if needed
            if (!_applyFilter ||
                (TargetAppData.Instance.TargetProcess is not null &&
                report.FilePath.Contains(TargetAppData.Instance.TargetProcess.ProcessName, StringComparison.OrdinalIgnoreCase)))
            {
                WatsonDisplayInfo displayInfo = new WatsonDisplayInfo(report);

                // Add the item in appropriate spot
                if (_currentCompareFunction is not null)
                {
                    int i = 0;
                    Debug.Assert(_currentSortAscending is not null, "Compare function is not null, but order is?");

                    // Add the item in appropriate spot
                    while (i < DisplayedReports.Count && _currentCompareFunction(DisplayedReports[i], displayInfo, _currentSortAscending ?? true) < 0)
                    {
                        i++;
                    }

                    DisplayedReports.Insert(i, new WatsonDisplayInfo(report));
                }
                else
                {
                    DisplayedReports.Add(new WatsonDisplayInfo(report));
                }
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

        _currentCompareFunction = CompareByFaultingExecutable;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByFaultingExecutable(WatsonDisplayInfo info1, WatsonDisplayInfo info2, bool sortAscending)
    {
        if (sortAscending)
        {
            return string.Compare(info1.Report.Executable, info2.Report.Executable, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return string.Compare(info2.Report.Executable, info1.Report.Executable, StringComparison.OrdinalIgnoreCase);
        }
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

        _currentCompareFunction = CompareByDateTime;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByDateTime(WatsonDisplayInfo info1, WatsonDisplayInfo info2, bool sortAscending)
    {
        if (sortAscending)
        {
            return string.Compare(info1.Report.TimeGenerated, info2.Report.TimeGenerated, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return string.Compare(info2.Report.TimeGenerated, info1.Report.TimeGenerated, StringComparison.OrdinalIgnoreCase);
        }
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

        _currentCompareFunction = CompareByWatsonBucket;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByWatsonBucket(WatsonDisplayInfo info1, WatsonDisplayInfo info2, bool sortAscending)
    {
        if (sortAscending)
        {
            return string.Compare(info1.FailureBucket, info2.FailureBucket, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return string.Compare(info2.FailureBucket, info1.FailureBucket, StringComparison.OrdinalIgnoreCase);
        }
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

        _currentCompareFunction = CompareByCrashDumpPath;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByCrashDumpPath(WatsonDisplayInfo info1, WatsonDisplayInfo info2, bool sortAscending)
    {
        if (sortAscending)
        {
            return string.Compare(info1.Report.CrashDumpPath, info2.Report.CrashDumpPath, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return string.Compare(info2.Report.CrashDumpPath, info1.Report.CrashDumpPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    [RelayCommand]
    private void RunAsAdmin()
    {
        if (TargetAppData.Instance.TargetProcess is not null)
        {
            CommonHelper.RunAsAdmin(TargetAppData.Instance.TargetProcess.Id, nameof(WatsonPageViewModel));
        }
    }

    public void ChangeLocalCollectionForApp(bool enable)
    {
        Process? process = TargetAppData.Instance.TargetProcess;

        if (process is null || process.ProcessName is null)
        {
            return;
        }

        string app = process.ProcessName + ".exe";

        if (enable == WatsonHelper.Instance.IsCollectionEnabledForApp(app))
        {
            // No change, could be initialization of the UI
            return;
        }

        Debug.Assert(RuntimeHelper.IsCurrentProcessRunningAsAdmin(), "Changing the local Watson dump collection for an app can only happen when running as admin.");

        if (enable)
        {
            WatsonHelper.Instance.EnableCollectionForApp(app);
        }
        else
        {
            WatsonHelper.Instance.DisableCollectionForApp(app);
        }
    }

    public void OpenCab(string file)
    {
        if (File.Exists(file))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = true,
            };

            Process.Start(startInfo);
        }
    }
}
