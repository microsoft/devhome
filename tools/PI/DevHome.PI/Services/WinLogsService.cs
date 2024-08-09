// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Services;

public partial class WinLogsService : ObservableObject, IDisposable
{
    public const string EtwLogsName = "ETW Logs";
    public const string DebugOutputLogsName = "DebugOutput";
    public const string EventViewerName = "EventViewer";
    public const string WERName = "WER";

    private readonly WERHelper _werHelper;
    private Process? _targetProcess;
    private ETWHelper? _etwHelper;
    private DebugMonitor? _debugMonitor;
    private EventViewerHelper? _eventViewerHelper;

    private Thread? _etwThread;
    private Thread? _debugMonitorThread;
    private Thread? _eventViewerThread;

    [ObservableProperty]
    private ObservableCollection<WinLogsEntry> _winLogEntries;

    public WinLogsService()
    {
        _winLogEntries = [];
        _werHelper = Application.Current.GetService<WERHelper>();
    }

    public void Start(Process process, bool isEtwEnabled, bool isDebugOutputEnabled, bool isEventViewerEnabled, bool isWEREnabled)
    {
        _targetProcess = process;
        Debug.Assert(_targetProcess is not null, "Target Process cannot be null while starting logs");

        if (isEtwEnabled)
        {
            StartETWLogsThread();
        }

        if (isDebugOutputEnabled)
        {
            StartDebugOutputsThread();
        }

        if (isEventViewerEnabled)
        {
            StartEventViewerThread();
        }

        if (isWEREnabled)
        {
            StartWER();
        }
    }

    public void Stop()
    {
        // Stop ETW logs
        StopETWLogsThread();

        // Stop Debug Outputs
        StopDebugOutputsThread();

        // Stop Event Viewer
        StopEventViewerThread();

        // Stop WER
        StopWER();
    }

    public void Clear()
    {
        WinLogEntries.Clear();
    }

    public void AddWinLogsEntry(WinLogsEntry entry)
    {
        WinLogEntries.Add(entry);
    }

    public void Dispose()
    {
        _etwHelper?.Dispose();
        _debugMonitor?.Dispose();
        _eventViewerHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void StartETWLogsThread()
    {
        // Stop and close existing thread if any
        StopETWLogsThread();

        // Start a new thread
        _etwThread = new Thread(() =>
        {
            Debug.Assert(_targetProcess is not null, "Target Process cannot be null while starting ETW logs");
            _etwHelper?.Dispose();
            _etwHelper = new ETWHelper(_targetProcess);
            _etwHelper.Start();
        });
        _etwThread.Name = EtwLogsName + " Thread";
        _etwThread.Start();
    }

    private void StopETWLogsThread()
    {
        _etwHelper?.Stop();

        if (Thread.CurrentThread != _etwThread)
        {
            _etwThread?.Join();
        }
    }

    private void StartDebugOutputsThread()
    {
        // Stop and close existing thread if any
        StopDebugOutputsThread();

        // Start a new thread
        _debugMonitorThread = new Thread(() =>
        {
            Debug.Assert(_targetProcess is not null, "Target Process cannot be null while starting DebugMonitor logs");
            _debugMonitor?.Dispose();
            _debugMonitor = new DebugMonitor(_targetProcess);
            _debugMonitor.Start();
        });
        _debugMonitorThread.Name = DebugOutputLogsName + " Thread";
        _debugMonitorThread.Start();
    }

    private void StopDebugOutputsThread()
    {
        _debugMonitor?.Stop();

        if (Thread.CurrentThread != _debugMonitorThread)
        {
            _debugMonitorThread?.Join();
        }
    }

    private void StartEventViewerThread()
    {
        // Stop and close existing thread if any
        StopEventViewerThread();

        // Start a new thread
        _eventViewerThread = new Thread(() =>
        {
            // Start EventViewer logs
            Debug.Assert(_targetProcess is not null, "Target Process cannot be null while starting EventViewer logs");
            _eventViewerHelper?.Dispose();
            _eventViewerHelper = new EventViewerHelper(_targetProcess);
            _eventViewerHelper.Start();
        });
        _eventViewerThread.Name = EventViewerName + " Thread";
        _eventViewerThread.Start();
    }

    private void StopEventViewerThread()
    {
        _eventViewerHelper?.Stop();

        if (Thread.CurrentThread != _eventViewerThread)
        {
            _eventViewerThread?.Join();
        }
    }

    private void StartWER()
    {
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WEREvents_CollectionChanged;
    }

    private void StopWER()
    {
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged -= WEREvents_CollectionChanged;
    }

    private void WEREvents_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (WERReport report in e.NewItems)
            {
                var filePath = report.Executable ?? string.Empty;

                // Filter WER events based on the process we're targeting
                if ((_targetProcess is not null) && filePath.Contains(_targetProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    WinLogsEntry entry = new(report.TimeStamp, WinLogCategory.Error, report.Description, WinLogsService.WERName);
                    WinLogEntries.Add(entry);
                }
            }
        }
    }

    public void LogStateChanged(WinLogsTool logType, bool isEnabled)
    {
        if (isEnabled)
        {
            switch (logType)
            {
                case WinLogsTool.ETWLogs:
                    StartETWLogsThread();
                    break;
                case WinLogsTool.DebugOutput:
                    StartDebugOutputsThread();
                    break;
                case WinLogsTool.EventViewer:
                    StartEventViewerThread();
                    break;
                case WinLogsTool.WER:
                    StartWER();
                    break;
            }
        }
        else
        {
            switch (logType)
            {
                case WinLogsTool.ETWLogs:
                    StopETWLogsThread();
                    break;
                case WinLogsTool.DebugOutput:
                    StopDebugOutputsThread();
                    break;
                case WinLogsTool.EventViewer:
                    StopEventViewerThread();
                    break;
                case WinLogsTool.WER:
                    StopWER();
                    break;
            }
        }
    }

    public static WinLogCategory ConvertTraceEventLevelToWinLogCategory(TraceEventLevel level)
    {
        var category = WinLogCategory.Information;

        switch (level)
        {
            case TraceEventLevel.Error:
            case TraceEventLevel.Critical:
                category = WinLogCategory.Error;
                break;
            case TraceEventLevel.Warning:
                category = WinLogCategory.Warning;
                break;
        }

        return category;
    }

    public static WinLogCategory ConvertStandardEventLevelToWinLogCategory(byte? level)
    {
        var category = WinLogCategory.Information;

        if (level.HasValue)
        {
            StandardEventLevel standardEventLevel = (StandardEventLevel)level.Value;
            switch (standardEventLevel)
            {
                case StandardEventLevel.Error:
                case StandardEventLevel.Critical:
                    category = WinLogCategory.Error;
                    break;
                case StandardEventLevel.Warning:
                    category = WinLogCategory.Warning;
                    break;
            }
        }

        return category;
    }
}
