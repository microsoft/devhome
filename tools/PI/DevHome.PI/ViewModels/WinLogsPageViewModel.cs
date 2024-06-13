// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
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
using DevHome.PI.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.ViewModels;

public partial class WinLogsPageViewModel : ObservableObject, IDisposable
{
    private readonly bool logMeasures;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    [ObservableProperty]
    private ObservableCollection<WinLogsEntry> winLogEntries;

    [ObservableProperty]
    private Visibility insightsButtonVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility runAsAdminVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility gridVisibility = Visibility.Visible;

    [ObservableProperty]
    private bool isETWLogsEnabled;

    [ObservableProperty]
    private bool isDebugOutputEnabled;

    [ObservableProperty]
    private bool isEventViewerEnabled = true;

    [ObservableProperty]
    private bool isWatsonEnabled = true;

    private Process? targetProcess;
    private WinLogsHelper? winLogsHelper;

    public WinLogsPageViewModel()
    {
        // Log feature usage.
        logMeasures = true;
        App.Log("DevHome.PI_WinLogs_PageInitialize", LogLevel.Measure);

        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        winLogEntries = new();

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    private void UpdateTargetProcess(Process process)
    {
        if (targetProcess != process)
        {
            targetProcess = process;
            GridVisibility = Visibility.Visible;
            RunAsAdminVisibility = Visibility.Collapsed;
            StopWinLogs();

            try
            {
                if (!process.HasExited)
                {
                    IsETWLogsEnabled = ETWHelper.IsUserInPerformanceLogUsersGroup();
                    winLogsHelper = new WinLogsHelper(targetProcess);
                    winLogsHelper.Start(IsETWLogsEnabled, IsDebugOutputEnabled, IsEventViewerEnabled, IsWatsonEnabled);
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
        winLogsHelper?.Stop();

        if (shouldCleanLogs)
        {
            ClearWinLogs();
        }
    }

    private void FindPattern(string message)
    {
        var newInsight = InsightsHelper.FindPattern(message);

        dispatcher.TryEnqueue(() =>
        {
            if (newInsight is not null)
            {
                newInsight.IsExpanded = true;
                var insightsPageViewModel = Application.Current.GetService<InsightsPageViewModel>();
                insightsPageViewModel.AddInsight(newInsight);
                InsightsButtonVisibility = Visibility.Visible;
            }
            else
            {
                InsightsButtonVisibility = Visibility.Collapsed;
            }
        });
    }

    public void LogStateChanged(object sender, RoutedEventArgs e)
    {
        var box = e.OriginalSource as CheckBox;
        if ((box is not null) && (box.Tag is not null))
        {
            var isChecked = box.IsChecked;

            if (logMeasures)
            {
                App.Log("DevHome.PI_WinLogs_LogStateChanged", LogLevel.Measure, new LogStateChangedEventData(box.Name, (box.IsChecked ?? false) ? "true" : "false"), null);
            }

            var tool = (WinLogsTool)box.Tag;
            winLogsHelper?.LogStateChanged(tool, isChecked ?? false);
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

    public void AddNewEntry(DateTime? time, WinLogCategory category, string message, string toolName)
    {
        var newEntry = new WinLogsEntry(time, category, message, toolName);
        dispatcher.TryEnqueue(() =>
        {
            WinLogEntries.Add(newEntry, entry => entry.DateTimeGenerated);
            ThreadPool.QueueUserWorkItem((o) => FindPattern(newEntry.Message));
        });
    }

    public void Dispose()
    {
        winLogsHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void ClearWinLogs()
    {
        if (logMeasures)
        {
            // Log feature usage.
            App.Log("DevHome.PI_WinLogs_ClearLogs", LogLevel.Measure);
        }

        dispatcher.TryEnqueue(() =>
        {
            WinLogEntries.Clear();

            InsightsButtonVisibility = Visibility.Collapsed;
        });
    }

    [RelayCommand]
    private void ShowInsightsPage()
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.NavigateTo(typeof(InsightsPageViewModel));
    }

    [RelayCommand]
    private void RunAsAdmin()
    {
        if (targetProcess is not null)
        {
            CommonHelper.RunAsAdmin(targetProcess.Id, nameof(WinLogsPageViewModel));
        }
    }
}
