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
            // Process.Start(file);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = true,
            };

            Process.Start(startInfo);
        }
    }
}
