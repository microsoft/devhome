// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using DevHome.PI.Models;
using Microsoft.Diagnostics.Tracing;

namespace DevHome.PI.Helpers;

public class WinLogsHelper : IDisposable
{
    public const string EtwLogsName = "ETW Logs";
    public const string DebugOutputLogsName = "DebugOutput";
    public const string EventViewerName = "EventViewer";
    public const string WatsonName = "Watson";

    private readonly ETWHelper etwHelper;
    private readonly DebugMonitor debugMonitor;
    private readonly EventViewerHelper eventViewerHelper;
    private readonly WatsonHelper watsonHelper;

    private Thread? etwThread;
    private Thread? debugMonitorThread;
    private Thread? eventViewerThread;
    private Thread? watsonThread;

    public WinLogsHelper(Process targetProcess)
    {
        // Initialize ETW logs
        etwHelper = new ETWHelper(targetProcess);

        // Initialize DebugMonitor
        debugMonitor = new DebugMonitor(targetProcess);

        // Initialize EventViewer
        eventViewerHelper = new EventViewerHelper(targetProcess);

        // Initialize Watson
        watsonHelper = new WatsonHelper(targetProcess);
    }

    public void Start(bool isETWEnabled, bool isDebugMonitorEnabled, bool isEventViewerEnabled, bool isWatsonEnabled)
    {
        if (isETWEnabled)
        {
            StartETWLogsThread();
        }

        if (isDebugMonitorEnabled)
        {
            StartDebugOutputsThread();
        }

        if (isEventViewerEnabled)
        {
            StartEventViewerThread();
        }

        if (isWatsonEnabled)
        {
            StartWatsonThread();
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

        // Stop Watson
        StopWatsonThread();
    }

    public void Dispose()
    {
        etwHelper.Dispose();
        debugMonitor.Dispose();
        eventViewerHelper.Dispose();
        watsonHelper.Dispose();
        GC.SuppressFinalize(this);
    }

    private void StartETWLogsThread()
    {
        // Stop and close existing thread if any
        StopETWLogsThread();

        // Start a new thread
        etwThread = new Thread(() =>
        {
            etwHelper.Start();
        });
        etwThread.Name = EtwLogsName + " Thread";
        etwThread.Start();
    }

    private void StopETWLogsThread()
    {
        etwHelper.Stop();

        if (Thread.CurrentThread != etwThread)
        {
            etwThread?.Join();
        }
    }

    private void StartDebugOutputsThread()
    {
        // Stop and close existing thread if any
        StopDebugOutputsThread();

        // Start a new thread
        debugMonitorThread = new Thread(() =>
        {
            // Start Debug Outputs
            debugMonitor.Start();
        });
        debugMonitorThread.Name = DebugOutputLogsName + " Thread";
        debugMonitorThread.Start();
    }

    private void StopDebugOutputsThread()
    {
        debugMonitor.Stop();

        if (Thread.CurrentThread != debugMonitorThread)
        {
            debugMonitorThread?.Join();
        }
    }

    private void StartEventViewerThread()
    {
        // Stop and close existing thread if any
        StopEventViewerThread();

        // Start a new thread
        eventViewerThread = new Thread(() =>
        {
            // Start EventViewer logs
            eventViewerHelper.Start();
        });
        eventViewerThread.Name = EventViewerName + " Thread";
        eventViewerThread.Start();
    }

    private void StopEventViewerThread()
    {
        eventViewerHelper.Stop();

        if (Thread.CurrentThread != eventViewerThread)
        {
            eventViewerThread?.Join();
        }
    }

    private void StartWatsonThread()
    {
        // Stop and close existing thread if any
        StopWatsonThread();

        // Start a new thread
        watsonThread = new Thread(() =>
        {
            // Start Watson logs
            watsonHelper.Start();
        });
        watsonThread.Name = WatsonName + " Thread";
        watsonThread.Start();
    }

    private void StopWatsonThread()
    {
        watsonHelper.Stop();

        if (Thread.CurrentThread != watsonThread)
        {
            watsonThread?.Join();
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
                case WinLogsTool.Watson:
                    StartWatsonThread();
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
                case WinLogsTool.Watson:
                    StopWatsonThread();
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
