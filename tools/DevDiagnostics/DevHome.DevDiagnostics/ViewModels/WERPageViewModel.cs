// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Properties;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class WERPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly WERHelper _werHelper;
    private readonly WERAnalyzer _werAnalyzer;
    private Tool? _selectedAnalysisTool;

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

    [ObservableProperty]
    private AdvancedCollectionView _reportsView;

    public ReadOnlyObservableCollection<Tool> RegisteredAnalysisTools => _werAnalyzer.RegisteredAnalysisTools;

    public WERPageViewModel(WERAnalyzer werAnalyzer)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _reportsView = new AdvancedCollectionView(werAnalyzer.WERAnalysisReports, true);

        _werInfoText = string.Empty;
        _applyFilter = Settings.Default.ApplyAppFilteringToData;
        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        _selectedAnalysisTool = null;

        _werHelper = Application.Current.GetService<WERHelper>();

        RunningAsAdmin = RuntimeHelper.IsCurrentProcessRunningAsAdmin();
        AllowElevationOption = !RunningAsAdmin;

        string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;
        AttachedToApp = attachedApp is not null;
        LocalCollectionEnabledForApp = attachedApp is not null ? _werHelper.IsCollectionEnabledForApp(attachedApp + ".exe") : false;

        _werAnalyzer = werAnalyzer;
        ((INotifyCollectionChanged)_werAnalyzer.WERAnalysisReports).CollectionChanged += WER_CollectionChanged;

        _reportsView.Filter = entry => FilterReport((WERAnalysisReport)entry);
        _reportsView.SortDescriptions.Add(new SortDescription(
            nameof(WERAnalysisReport.Report),
            SortDirection.Descending,
            Comparer<WERReport>.Create((x, y) => x.TimeStamp.CompareTo(y.TimeStamp))));
        RefreshView();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.ApplyAppFilteringToData))
        {
            _applyFilter = Settings.Default.ApplyAppFilteringToData;
            RefreshView();
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            RefreshView();

            string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;
            AttachedToApp = attachedApp is not null;
            LocalCollectionEnabledForApp = attachedApp is not null ? _werHelper.IsCollectionEnabledForApp(attachedApp + ".exe") : false;
        }
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            RefreshView();
        }
    }

    private void RefreshView()
    {
        _dispatcher.TryEnqueue(() =>
        {
            ReportsView.Refresh();
        });
    }

    private bool FilterReport(WERAnalysisReport analysisReport)
    {
        if (_applyFilter)
        {
            return TargetAppData.Instance.TargetProcess is not null &&
                analysisReport.Report.FilePath.Contains(TargetAppData.Instance.TargetProcess.ProcessName, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    public void SetBucketingTool(Tool tool)
    {
        _selectedAnalysisTool = tool;
        foreach (WERAnalysisReport report in _werAnalyzer.WERAnalysisReports)
        {
            report.SetFailureBucketTool(tool);
        }
    }

    public void SortReports(object sender, DataGridColumnEventArgs e)
    {
        var propertyName = string.Empty;
        Comparer<WERReport>? comparer = null;
        if (e.Column.Tag is not null)
        {
            string? tag = e.Column.Tag.ToString();
            Debug.Assert(tag is not null, "Why is the tag null?");

            if (tag == "DateTime")
            {
                propertyName = nameof(WERAnalysisReport.Report);
                comparer = Comparer<WERReport>.Create((x, y) => x.TimeStamp.CompareTo(y.TimeStamp));
            }
            else if (tag == "FaultingExecutable")
            {
                propertyName = nameof(WERAnalysisReport.Report);
                comparer = Comparer<WERReport>.Create((x, y) => string.Compare(x.Executable, y.Executable, StringComparison.OrdinalIgnoreCase));
            }
            else if (tag == "WERBucket")
            {
                propertyName = nameof(WERAnalysisReport.FailureBucket);
            }
            else if (tag == "CrashDumpPath")
            {
                propertyName = nameof(WERAnalysisReport.Report);
                comparer = Comparer<WERReport>.Create((x, y) => string.Compare(x.CrashDumpPath, y.CrashDumpPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrEmpty(propertyName))
        {
            if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
            {
                // Clear pervious sorting
                ReportsView.SortDescriptions.Clear();
                ReportsView.SortDescriptions.Add(new SortDescription(propertyName, SortDirection.Ascending, comparer));
                e.Column.SortDirection = DataGridSortDirection.Ascending;
            }
            else
            {
                ReportsView.SortDescriptions.Clear();
                ReportsView.SortDescriptions.Add(new SortDescription(propertyName, SortDirection.Descending, comparer));
                e.Column.SortDirection = DataGridSortDirection.Descending;
            }
        }
    }

    [RelayCommand]
    public void ResetBucketingTool()
    {
        _selectedAnalysisTool = null;
        foreach (WERAnalysisReport report in _werAnalyzer.WERAnalysisReports)
        {
            report.SetFailureBucketTool(null);
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
