// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Services;
using DevHome.PI.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.ViewModels;

public partial class WinLogsPageViewModel : ObservableObject, IDisposable
{
    private readonly bool _logMeasures;
    private readonly ObservableCollection<WinLogsEntry> _winLogsOutput;
    private readonly DispatcherQueue _dispatcher;

    [ObservableProperty]
    private ObservableCollection<WinLogsEntry> _winLogEntries;

    [ObservableProperty]
    private Visibility _runAsAdminVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _gridVisibility = Visibility.Visible;

    [ObservableProperty]
    private bool _isETWLogsEnabled;

    [ObservableProperty]
    private bool _isDebugOutputEnabled;

    [ObservableProperty]
    private bool _isEventViewerEnabled = true;

    [ObservableProperty]
    private bool _isWEREnabled = true;

    private Process? _targetProcess;
    private WinLogsHelper? _winLogsHelper;

    public WinLogsPageViewModel()
    {
        // Log feature usage.
        _logMeasures = true;
        App.Log("DevHome.PI_WinLogs_PageInitialize", LogLevel.Measure);

        _dispatcher = DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _winLogEntries = [];
        _winLogsOutput = [];
        _winLogsOutput.CollectionChanged += WinLogsOutput_CollectionChanged;

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    public void UpdateTargetProcess(Process process)
    {
        if (_targetProcess != process)
        {
            _targetProcess = process;
            GridVisibility = Visibility.Visible;
            RunAsAdminVisibility = Visibility.Collapsed;
            StopWinLogs();

            try
            {
                if (!process.HasExited)
                {
                    IsETWLogsEnabled = ETWHelper.IsUserInPerformanceLogUsersGroup();
                    _winLogsHelper = new WinLogsHelper(_targetProcess, _winLogsOutput);
                    _winLogsHelper.Start(IsETWLogsEnabled, IsDebugOutputEnabled, IsEventViewerEnabled, IsWEREnabled);
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == (int)Windows.Win32.Foundation.WIN32_ERROR.ERROR_ACCESS_DENIED)
                {
                    GridVisibility = Visibility.Collapsed;

                    // Only show the button when not running as admin. This is possible when the target app is a system app.
                    if (!RuntimeHelper.IsCurrentProcessRunningAsAdmin())
                    {
                        RunAsAdminVisibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            if (TargetAppData.Instance.TargetProcess is not null)
            {
                UpdateTargetProcess(TargetAppData.Instance.TargetProcess);
            }
            else
            {
                StopWinLogs(false);
            }
        }
        else if (e.PropertyName == nameof(TargetAppData.HasExited))
        {
            StopWinLogs(false);
        }
    }

    private void StopWinLogs(bool shouldCleanLogs = true)
    {
        _winLogsHelper?.Stop();

        if (shouldCleanLogs)
        {
            ClearWinLogs();
        }
    }

    private void WinLogsOutput_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            _dispatcher.TryEnqueue(() =>
            {
                foreach (WinLogsEntry newEntry in e.NewItems)
                {
                    WinLogEntries.Add(newEntry);
                    ThreadPool.QueueUserWorkItem((o) => FindPattern(newEntry.Message));
                }
            });
        }
    }

    public void Dispose()
    {
        _winLogsHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void LogStateChanged(object sender, RoutedEventArgs e)
    {
        var box = e.OriginalSource as CheckBox;
        if ((box is not null) && (box.Tag is not null))
        {
            var isChecked = box.IsChecked;

            if (_logMeasures)
            {
                App.Log("DevHome.PI_WinLogs_LogStateChanged", LogLevel.Measure, new LogStateChangedEventData(box.Name, (box.IsChecked ?? false) ? "true" : "false"), null);
            }

            var tool = (WinLogsTool)box.Tag;
            _winLogsHelper?.LogStateChanged(tool, isChecked ?? false);
        }
    }

    public void UpdateClipboardContent(object sender, DataGridRowClipboardEventArgs e)
    {
        var winLogEntry = e.Item as WinLogsEntry;
        var dataGrid = sender as DataGrid;
        if ((winLogEntry is not null) && (dataGrid is not null))
        {
            // Message Column is the last column.
            var messageColumnIndex = dataGrid.Columns.Count - 1;
            var selectedColumnIndex = dataGrid.CurrentColumn.DisplayIndex;

            // Clear clipboard if the selected column is Message column
            // to copy only the selected text from the textbox.
            if ((winLogEntry.SelectedText.Length > 0) && (selectedColumnIndex == messageColumnIndex))
            {
                e.ClipboardRowContent.Clear();
            }
        }
    }

    private void FindPattern(string message)
    {
        var newInsight = InsightsHelper.FindPattern(message);
        if (newInsight is not null)
        {
            _dispatcher.TryEnqueue(() =>
            {
                var insightsService = Application.Current.GetService<PIInsightsService>();
                insightsService.AddInsight(newInsight);
            });
        }
    }

    [RelayCommand]
    private void ClearWinLogs()
    {
        if (_logMeasures)
        {
            // Log feature usage.
            App.Log("DevHome.PI_WinLogs_ClearLogs", LogLevel.Measure);
        }

        _winLogsOutput?.Clear();
        _dispatcher.TryEnqueue(() =>
        {
            WinLogEntries.Clear();
        });
    }

    [RelayCommand]
    private void RunAsAdmin()
    {
        if (_targetProcess is not null)
        {
            CommonHelper.RunAsAdmin(_targetProcess.Id, nameof(WinLogsPageViewModel));
        }
    }
}
