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
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using Microsoft.UI.Xaml;

namespace DevHome.PI.ViewModels;

public partial class WERPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly WERHelper _werHelper;

    [ObservableProperty]
    private ObservableCollection<WERDisplayInfo> _displayedReports = [];

    [ObservableProperty]
    private string _werInfoText;

    private bool _applyFilter = true;

    [ObservableProperty]
    private bool _attachedToApp;

    [ObservableProperty]
    private bool _localCollectionEnabledForApp;

    [ObservableProperty]
    private bool _runningAsAdmin;

    [ObservableProperty]
    private bool _allowElevationOption;

    private delegate int WERCompareFunction(WERDisplayInfo info1, WERDisplayInfo info2, bool sortAscending);

    private WERCompareFunction? _currentCompareFunction;
    private bool? _currentSortAscending;

    public WERPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _werInfoText = string.Empty;
        _applyFilter = Settings.Default.ApplyAppFilteringToData;
        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        _werHelper = Application.Current.GetService<WERHelper>();

        RunningAsAdmin = RuntimeHelper.IsCurrentProcessRunningAsAdmin();
        AllowElevationOption = !RunningAsAdmin;

        string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;
        AttachedToApp = attachedApp is not null;
        LocalCollectionEnabledForApp = attachedApp is not null ? _werHelper.IsCollectionEnabledForApp(attachedApp + ".exe") : false;

        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WER_CollectionChanged;

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
            LocalCollectionEnabledForApp = attachedApp is not null ? _werHelper.IsCollectionEnabledForApp(attachedApp + ".exe") : false;
        }
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            _dispatcher.TryEnqueue(() =>
            {
                FilterWERReportList(e.NewItems);
            });
        }
    }

    private void PopulateCurrentLogs()
    {
        _dispatcher.TryEnqueue(() =>
        {
            DisplayedReports.Clear();

            FilterWERReportList(_werHelper.WERReports.ToList<WERReport>());
        });
    }

    private void FilterWERReportList(System.Collections.IList? reportList)
    {
        if (reportList is null)
        {
            return;
        }

        // Get all existing reports
        foreach (WERReport report in reportList)
        {
            // Provide filtering if needed
            if (!_applyFilter ||
                (TargetAppData.Instance.TargetProcess is not null &&
                report.FilePath.Contains(TargetAppData.Instance.TargetProcess.ProcessName, StringComparison.OrdinalIgnoreCase)))
            {
                WERDisplayInfo displayInfo = new WERDisplayInfo(report);

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

                    DisplayedReports.Insert(i, new WERDisplayInfo(report));
                }
                else
                {
                    DisplayedReports.Add(new WERDisplayInfo(report));
                }
            }
        }
    }

    internal void SortByFaultingExecutable(bool sortAscending)
    {
        ObservableCollection<WERDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderBy(x => x.Report.Executable));
        }
        else
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderByDescending(x => x.Report.Executable));
        }

        DisplayedReports = sortedCollection;

        _currentCompareFunction = CompareByFaultingExecutable;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByFaultingExecutable(WERDisplayInfo info1, WERDisplayInfo info2, bool sortAscending)
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
        ObservableCollection<WERDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderBy(x => x.Report.TimeGenerated));
        }
        else
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderByDescending(x => x.Report.TimeGenerated));
        }

        DisplayedReports = sortedCollection;

        _currentCompareFunction = CompareByDateTime;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByDateTime(WERDisplayInfo info1, WERDisplayInfo info2, bool sortAscending)
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

    internal void SortByWERBucket(bool sortAscending)
    {
        ObservableCollection<WERDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderBy(x => x.FailureBucket));
        }
        else
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderByDescending(x => x.FailureBucket));
        }

        DisplayedReports = sortedCollection;

        _currentCompareFunction = CompareByWERBucket;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByWERBucket(WERDisplayInfo info1, WERDisplayInfo info2, bool sortAscending)
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
        ObservableCollection<WERDisplayInfo> sortedCollection;

        if (sortAscending)
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderBy(x => x.Report.CrashDumpPath));
        }
        else
        {
            sortedCollection = new ObservableCollection<WERDisplayInfo>(DisplayedReports.OrderByDescending(x => x.Report.CrashDumpPath));
        }

        DisplayedReports = sortedCollection;

        _currentCompareFunction = CompareByCrashDumpPath;
        _currentSortAscending = sortAscending;
    }

    internal int CompareByCrashDumpPath(WERDisplayInfo info1, WERDisplayInfo info2, bool sortAscending)
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
            CommonHelper.RunAsAdmin(TargetAppData.Instance.TargetProcess.Id, nameof(WERPageViewModel));
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

        if (enable == _werHelper.IsCollectionEnabledForApp(app))
        {
            // No change, could be initialization of the UI
            return;
        }

        Debug.Assert(RuntimeHelper.IsCurrentProcessRunningAsAdmin(), "Changing the local dump collection for an app can only happen when running as admin.");

        if (enable)
        {
            _werHelper.EnableCollectionForApp(app);
        }
        else
        {
            _werHelper.DisableCollectionForApp(app);
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
