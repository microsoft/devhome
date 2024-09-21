// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Properties;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class WERPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly WERHelper _werHelper;
    private readonly WERAnalyzer _werAnalyzer;

    private readonly string _localDumpEnableButtonText = CommonHelper.GetLocalizedString("LocalDumpCollectionEnable");
    private readonly string _localDumpDisableButtonText = CommonHelper.GetLocalizedString("LocalDumpCollectionDisable");
    private readonly string _localDumpEnableButtonToolTip = CommonHelper.GetLocalizedString("LocalDumpCollectionEnableToolTip");
    private readonly string _localDumpDisableButtonToolTip = CommonHelper.GetLocalizedString("LocalDumpCollectionDisableToolTip");

    [ObservableProperty]
    private string _localDumpEnableDisableButtonToolTip;

    [ObservableProperty]
    private string _werInfoText;

    private bool _applyFilter = true;

    [ObservableProperty]
    private bool _attachedToApp;

    [ObservableProperty]
    private string _localDumpEnableDisableButtonText;

    [ObservableProperty]
    private AdvancedCollectionView _reportsView;

    public WERPageViewModel(WERAnalyzer werAnalyzer)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _reportsView = new AdvancedCollectionView(werAnalyzer.WERReports, true);

        _werInfoText = string.Empty;
        _applyFilter = Settings.Default.ApplyAppFilteringToData;
        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        _werHelper = Application.Current.GetService<WERHelper>();

        string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;
        AttachedToApp = attachedApp is not null;
        UpdateEnableDisableLocalDumpsButton();
        Debug.Assert(_localDumpEnableDisableButtonText is not null, "UpdateEnableDisableLocalDumpsButton should set this");
        Debug.Assert(_localDumpEnableDisableButtonToolTip is not null, "UpdateEnableDisableLocalDumpsButton should set this");

        _werAnalyzer = werAnalyzer;
        ((INotifyCollectionChanged)_werAnalyzer.WERReports).CollectionChanged += WER_CollectionChanged;

        _reportsView.Filter = entry => FilterReport((WERReport)entry);

        _reportsView.SortDescriptions.Add(new SortDescription(
            SortDirection.Descending,
            Comparer<WERReport>.Create((x, y) => x.BasicReport.TimeStamp.CompareTo(y.BasicReport.TimeStamp))));
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
            UpdateEnableDisableLocalDumpsButton();
        }
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            RefreshView();
        }
    }

    private void UpdateEnableDisableLocalDumpsButton()
    {
        string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;

        bool localCollectionEnabledForApp = attachedApp is not null ? WERUtils.IsCollectionEnabledForApp(attachedApp + ".exe") : false;
        LocalDumpEnableDisableButtonText = localCollectionEnabledForApp ? _localDumpDisableButtonText : _localDumpEnableButtonText;
        LocalDumpEnableDisableButtonToolTip = localCollectionEnabledForApp ? _localDumpDisableButtonToolTip : _localDumpEnableButtonToolTip;
    }

    private void RefreshView()
    {
        _dispatcher.TryEnqueue(() =>
        {
            ReportsView.Refresh();
        });
    }

    private bool FilterReport(WERReport analysisReport)
    {
        if (_applyFilter)
        {
            return TargetAppData.Instance.TargetProcess is not null &&
                analysisReport.BasicReport.FilePath.Contains(TargetAppData.Instance.TargetProcess.ProcessName, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    public void SortReports(object sender, DataGridColumnEventArgs e)
    {
        Comparer<WERReport>? comparer = null;
        if (e.Column.Tag is not null)
        {
            string? tag = e.Column.Tag.ToString();
            Debug.Assert(tag is not null, "Why is the tag null?");

            if (tag == "DateTime")
            {
                comparer = Comparer<WERReport>.Create((x, y) => x.BasicReport.TimeStamp.CompareTo(y.BasicReport.TimeStamp));
            }
            else if (tag == "FaultingExecutable")
            {
                comparer = Comparer<WERReport>.Create((x, y) => string.Compare(x.BasicReport.Executable, y.BasicReport.Executable, StringComparison.OrdinalIgnoreCase));
            }
            else if (tag == "WERBucket")
            {
                comparer = Comparer<WERReport>.Create((x, y) => string.Compare(x.FailureBucket, y.FailureBucket, StringComparison.OrdinalIgnoreCase));
            }
            else if (tag == "CrashDumpPath")
            {
                comparer = Comparer<WERReport>.Create((x, y) => string.Compare(x.BasicReport.CrashDumpPath, y.BasicReport.CrashDumpPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
        {
            // Clear pervious sorting
            ReportsView.SortDescriptions.Clear();
            ReportsView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, comparer));
            e.Column.SortDirection = DataGridSortDirection.Ascending;
        }
        else
        {
            ReportsView.SortDescriptions.Clear();
            ReportsView.SortDescriptions.Add(new SortDescription(SortDirection.Descending, comparer));
            e.Column.SortDirection = DataGridSortDirection.Descending;
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

    [RelayCommand]
    public void ToggleLocalCabCollection()
    {
        string? attachedApp = TargetAppData.Instance?.TargetProcess?.ProcessName ?? null;

        if (attachedApp is null)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem((o) =>
        {
            App.Log("ToggleLocalCabCollection", LogLevel.Measure);

            bool fEnabled = WERUtils.IsCollectionEnabledForApp(attachedApp + ".exe");

            try
            {
                FileInfo fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);

                var startInfo = new ProcessStartInfo()
                {
                    FileName = "EnableLocalCabCollection.exe",
                    Arguments = attachedApp + ".exe",
                    UseShellExecute = true,
                    WorkingDirectory = fileInfo.DirectoryName,
                    Verb = "runas",
                };

                var process = Process.Start(startInfo);
                if (process is not null)
                {
                    // Wait for the process to update registry keys
                    process.WaitForExit();

                    bool fEnabledAfterProcessLaunch = WERUtils.IsCollectionEnabledForApp(attachedApp + ".exe");

                    if (fEnabledAfterProcessLaunch == fEnabled)
                    {
                        App.Log("ToggleLocalCabCollectionFailure", LogLevel.Measure);
                    }

                    _dispatcher.TryEnqueue(() =>
                    {
                        UpdateEnableDisableLocalDumpsButton();
                    });
                }
            }
            catch (Exception ex)
            {
                // This isn't necessarily bad... the user could deny the UAC, and in those cases, this would be
                // a by-design failure.
                Log.Warning(ex.Message);
                App.Log("ToggleLocalCabCollectionFailure", LogLevel.Measure);
            }
        });
    }
}
