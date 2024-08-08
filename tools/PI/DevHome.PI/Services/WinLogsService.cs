// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Services;

public class WinLogsService : IDisposable
{
    public const string EtwLogsName = "ETW Logs";
    public const string DebugOutputLogsName = "DebugOutput";
    public const string EventViewerName = "EventViewer";
    public const string WERName = "WER";

    private readonly ETWHelper _etwHelper;
    private readonly DebugMonitor _debugMonitor;
    private readonly EventViewerHelper _eventViewerHelper;
    private readonly ObservableCollection<WinLogsEntry> _output;
    private readonly Process _targetProcess;
    private readonly WERHelper _werHelper;

    private Thread? _etwThread;
    private Thread? _debugMonitorThread;
    private Thread? _eventViewerThread;

    public WinLogsService(Process targetProcess, ObservableCollection<WinLogsEntry> output)
    {
        _targetProcess = targetProcess;
        _output = output;

        // Initialize ETW logs
        _etwHelper = new ETWHelper(targetProcess, output);

        // Initialize DebugMonitor
        _debugMonitor = new DebugMonitor(targetProcess, output);

        // Initialize EventViewer
        _eventViewerHelper = new EventViewerHelper(targetProcess, output);

        _werHelper = Application.Current.GetService<WERHelper>();
    }

    public void Start(bool isEtwEnabled, bool isDebugOutputEnabled, bool isEventViewerEnabled, bool isWEREnabled)
    {
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
            ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WEREvents_CollectionChanged;
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
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged -= WEREvents_CollectionChanged;
    }

    public void Dispose()
    {
        _etwHelper.Dispose();
        _debugMonitor.Dispose();
        _eventViewerHelper.Dispose();
        GC.SuppressFinalize(this);
    }

    private void StartETWLogsThread()
    {
        // Stop and close existing thread if any
        StopETWLogsThread();

        // Start a new thread
        _etwThread = new Thread(() =>
        {
            _etwHelper.Start();
        });
        _etwThread.Name = EtwLogsName + " Thread";
        _etwThread.Start();
    }

    private void StopETWLogsThread()
    {
        _etwHelper.Stop();

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
            // Start Debug Outputs
            _debugMonitor.Start();
        });
        _debugMonitorThread.Name = DebugOutputLogsName + " Thread";
        _debugMonitorThread.Start();
    }

    private void StopDebugOutputsThread()
    {
        _debugMonitor.Stop();

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
            _eventViewerHelper.Start();
        });
        _eventViewerThread.Name = EventViewerName + " Thread";
        _eventViewerThread.Start();
    }

    private void StopEventViewerThread()
    {
        _eventViewerHelper.Stop();

        if (Thread.CurrentThread != _eventViewerThread)
        {
            _eventViewerThread?.Join();
        }
    }

    private void WEREvents_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (WERReport report in e.NewItems)
            {
                var filePath = report.Executable ?? string.Empty;

                // Filter WER events based on the process we're targeting
                if (filePath.Contains(_targetProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    WinLogsEntry entry = new(report.TimeStamp, WinLogCategory.Error, report.Description, WinLogsService.WERName);
                    _output.Add(entry);
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
                    ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WEREvents_CollectionChanged;
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
                    ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged -= WEREvents_CollectionChanged;
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
