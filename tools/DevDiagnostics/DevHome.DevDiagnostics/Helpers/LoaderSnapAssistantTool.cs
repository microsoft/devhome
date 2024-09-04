// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Services;
using DevHome.Service;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Windows.Win32.Foundation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DevHome.DevDiagnostics.Helpers;

public class LoaderSnapAssistantTool
{
    private const string WindowsImageETWProvider = "2cb15d1d-5fc1-11d2-abe1-00a0c911f518"; /*EP_Microsoft-Windows-ImageLoad*/
    private const uint LoaderSnapsFlag = 0x80; /* ETW_UMGL_LDR_SNAPS_FLAG */
    private readonly DDInsightsService _insightsService;
    private readonly IDevHomeService _devHomeService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    public LoaderSnapAssistantTool()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _insightsService = Application.Current.GetService<DDInsightsService>();
        _devHomeService = Application.Current.GetService<IDevHomeService>();
        Init();
    }

    private void Init()
    {
        var crashDumpAnalyzerThread = new Thread(() =>
        {
            MyETWListener();
        });
        crashDumpAnalyzerThread.Name = "LoaderSnapAssistantThread";
        crashDumpAnalyzerThread.Start();

        _devHomeService.MissingFileProcessLaunchFailure += info =>
        {
            if (!IsLoaderSnapLoggingEnabledForImage(info.processName))
            {
                _dispatcher.TryEnqueue(() =>
                {
                    var insight = new InsightPossibleLoaderIssue();
                    insight.Title = "Process exited due to missing files";
                    insight.Text = string.Format(CultureInfo.CurrentCulture, "Process {0} (pid: {1,6}) exited with error code {2}. Enabling loader snaps can help diagnose why the app exited", info.processName, info.pid, info.exitCode);
                    insight.ImageFileName = info.processName;
                    _insightsService.AddInsight(insight);
                });
            }
        };
    }

    private void MyETWListener()
    {
        TraceEventSession session = new TraceEventSession("LoaderSnapAssistantSession");

        // Enable the loader snaps provider
        session.EnableProvider(WindowsImageETWProvider, TraceEventLevel.Error, LoaderSnapsFlag);

        // We don't care about a lot of the ETW data that is coming in, so we just hook up the All event and ignore it
        session.Source.Dynamic.All += data => { };

        // None of the loadersnap events are handled by the TraceEventParser, so we need to handle them ourselves
        session.Source.UnhandledEvents += UnHandledEventsHandler;
        session.Source.Process();
    }

    private void UnHandledEventsHandler(TraceEvent traceEvent)
    {
        if (traceEvent.EventName.Contains("Opcode(215)"))
        {
            byte[] loaderSnapData = traceEvent.EventData();
            string s = System.Text.Encoding.Unicode.GetString(loaderSnapData.Skip(10).ToArray());
            s = s.Replace("\n\0", string.Empty);
            s = s.Replace("\0", ": ");
            if (s.Contains("LdrpProcessWork - ERROR: Unable to load"))
            {
                string processName = traceEvent.ProcessName;
                int pid = traceEvent.ProcessID;
                _dispatcher.TryEnqueue(() =>
                {
                    var insight = new SimpleTextInsight();
                    insight.Title = string.Format(CultureInfo.CurrentCulture, "Process {0} (DDD: {1,6}) exited due to missing files", processName, pid);
                    insight.Description = s;
                    _insightsService.AddInsight(insight);
                });
            }
        }
    }

    private bool IsLoaderSnapLoggingEnabledForImage(string imageFileName)
    {
        // Check if the following registry key exists
        // Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\imageFileName
        // TracingFlags = 0x4
        RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + imageFileName, false);

        if (key is null)
        {
            return false;
        }

        if (key.GetValue("TracingFlags") is not int tracingFlags)
        {
            return false;
        }

        if ((tracingFlags & 0x4) != 0x4)
        {
            return false;
        }

        return true;
    }
}
