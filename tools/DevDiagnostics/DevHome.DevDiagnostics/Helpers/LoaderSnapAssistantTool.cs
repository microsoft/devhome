// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Controls;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Services;
using DevHome.DevDiagnostics.TelemetryEvents;
using DevHome.Service;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Serilog;

namespace DevHome.DevDiagnostics.Helpers;

public class LoaderSnapAssistantTool
{
    private const string LoaderSnapsIdentifyNoLogsTelemetryName = "LoaderSnapsIdentifyNoLogs";
    private const string LoaderSnapsIdentifyLogsTelemetryName = "LoaderSnapsIdentifyLogs";

    private const string WindowsImageETWProvider = "2cb15d1d-5fc1-11d2-abe1-00a0c911f518"; /*EP_Microsoft-Windows-ImageLoad*/
    private const uint LoaderSnapsFlag = 0x80; /* ETW_UMGL_LDR_SNAPS_FLAG */
    private const string LoaderSnapsETWOpCode = "Opcode(215)";
    private const string LoaderSnapsETWErrorLine = "LdrpProcessWork - ERROR: Unable to load";

    private readonly DDInsightsService _insightsService;
    private readonly IDevHomeService _devHomeService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private struct LoaderSnapFailure
    {
        public int Pid { get; set; }

        public string? ErrorLog { get; set; }

        public string? ImageName { get; set; }
    }

    private readonly Dictionary<int, LoaderSnapFailure> _loaderSnapFailures = new();

    [Flags]
    private enum TraceFlags
    {
        HeapTracing = 1,
        CritSecTracing = 2,
        LoaderSnaps = 4,
    }

    public LoaderSnapAssistantTool()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Debug.Assert(_dispatcher is not null, "Be sure to create this object on the UI thread");
        _insightsService = Application.Current.GetService<DDInsightsService>();
        _devHomeService = Application.Current.GetService<IDevHomeService>();
        Init();
    }

    private void Init()
    {
        var crashDumpAnalyzerThread = new Thread(LoaderSnapETWListener);
        crashDumpAnalyzerThread.Name = "LoaderSnapAssistantThread";
        crashDumpAnalyzerThread.Start();

        _devHomeService.MissingFileProcessLaunchFailure += info =>
        {
            if (!IsLoaderSnapLoggingEnabledForImage(info.processName))
            {
                App.Log(LoaderSnapsIdentifyNoLogsTelemetryName, LogLevel.Measure);

                _dispatcher.TryEnqueue(() =>
                {
                    var insight = new InsightPossibleLoaderIssue();
                    insight.Title = CommonHelper.GetLocalizedString("InsightProcessExitMissingDependenciesTitle");
                    insight.Text = string.Format(CultureInfo.CurrentCulture, CommonHelper.GetLocalizedString("InsightProcessExitMissingDependencies"), info.processName, info.pid, info.exitCode);
                    insight.ImageFileName = info.processName;
                    _insightsService.AddInsight(insight);
                });
            }
            else
            {
                lock (_loaderSnapFailures)
                {
                    if (_loaderSnapFailures.TryGetValue(info.pid, out LoaderSnapFailure loadersnapError))
                    {
                        // We had previously received information about this app's loader snap issue but were waiting for the image name.
                        // We can raise the notification now
                        _loaderSnapFailures.Remove(info.pid);
                        loadersnapError.ImageName = info.processName;
                        RaiseLoaderSnapInsight(loadersnapError);
                    }
                    else
                    {
                        // We haven't received the loader snap failure yet. Store the process name, and we'll raise the insight
                        // when we receive the loader snap logs
                        LoaderSnapFailure failure = default(LoaderSnapFailure);
                        failure.Pid = info.pid;
                        failure.ImageName = info.processName;
                        _loaderSnapFailures.Add(info.pid, failure);
                    }
                }
            }
        };
    }

    private void LoaderSnapETWListener()
    {
        using TraceEventSession session = new TraceEventSession("LoaderSnapAssistantSession");
        session.StopOnDispose = true;

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
        if (traceEvent.EventName.Contains(LoaderSnapsETWOpCode))
        {
            byte[] loaderSnapData = traceEvent.EventData();
            string s = System.Text.Encoding.Unicode.GetString(loaderSnapData.Skip(10).ToArray());

            // The loader snap data has embedded nulls in its string. Get rid of the embedded nulls that signify newlines, and
            // for the remaining ones, swap them out for colons
            s = s.Replace("\n\0", string.Empty);
            s = s.Replace("\0", ": ");
            if (s.Contains(LoaderSnapsETWErrorLine))
            {
                lock (_loaderSnapFailures)
                {
                    if (_loaderSnapFailures.TryGetValue(traceEvent.ProcessID, out LoaderSnapFailure loadersnapError))
                    {
                        // We had previously received information about this app's loader snap issue but were waiting for the image name.
                        // We can raise the notification now
                        _loaderSnapFailures.Remove(traceEvent.ProcessID);
                        loadersnapError.ErrorLog = s;
                        RaiseLoaderSnapInsight(loadersnapError);
                    }
                    else
                    {
                        // At this point, we don't have the faulting process's executable name. Wait until we get the callback
                        // from our service that tells of the process termination, and then we'll raise the insight.
                        LoaderSnapFailure failure = default(LoaderSnapFailure);
                        failure.Pid = traceEvent.ProcessID;
                        failure.ErrorLog = s;
                        _loaderSnapFailures.Add(traceEvent.ProcessID, failure);
                    }
                }
            }
        }
    }

    private void RaiseLoaderSnapInsight(LoaderSnapFailure failure)
    {
        App.Log(LoaderSnapsIdentifyLogsTelemetryName, LogLevel.Measure);

        _dispatcher.TryEnqueue(() =>
        {
            var insight = new SimpleTextInsight();
            insight.Title = string.Format(CultureInfo.CurrentCulture, CommonHelper.GetLocalizedString("InsightProcessExitMissingDependenciesIdentifiedTitle"), failure.ImageName, failure.Pid);
            Debug.Assert(!string.IsNullOrEmpty(failure.ErrorLog), "We should have an error log");
            insight.Description = failure.ErrorLog;
            _insightsService.AddInsight(insight);
        });
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

        if (!((TraceFlags)tracingFlags).HasFlag(TraceFlags.LoaderSnaps))
        {
            return false;
        }

        return true;
    }
}

public class InsightPossibleLoaderIssue : Insight
{
    private const string LoaderSnapsEnableLogsTelemetryName = "LoaderSnapsEnableLogs";

    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalTool));
    private readonly InsightForMissingFileProcessTerminationControl _mycontrol = new();
    private string _text = string.Empty;

    internal string Text
    {
        get => _text;

        set
        {
            _text = value;
            _mycontrol.Text = value;
        }
    }

    internal string ImageFileName { get; set; } = string.Empty;

    internal InsightPossibleLoaderIssue()
    {
        _mycontrol.Command = new RelayCommand(ConfigureLoaderSnaps);
        CustomControl = _mycontrol;
    }

    public void ConfigureLoaderSnaps()
    {
        App.Log(LoaderSnapsEnableLogsTelemetryName, LogLevel.Measure);

        try
        {
            FileInfo fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);

            var startInfo = new ProcessStartInfo()
            {
                FileName = "EnableLoaderSnaps.exe",
                Arguments = ImageFileName,
                UseShellExecute = true,
                WorkingDirectory = fileInfo.DirectoryName,
                Verb = "runas",
            };

            var process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
        }
    }
}
