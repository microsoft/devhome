// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
using DevHome.Service;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.UI.Xaml;
using Microsoft.Win32;

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
                    insight.Title = CommonHelper.GetLocalizedString("InsightProcessExitMissingDependenciesTitle");
                    insight.Text = string.Format(CultureInfo.CurrentCulture, CommonHelper.GetLocalizedString("InsightProcessExitMissingDependencies"), info.processName, info.pid, info.exitCode);
                    insight.ImageFileName = info.processName;
                    _insightsService.AddInsight(insight);
                });
            }
        };
    }

    private void MyETWListener()
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
                    insight.Title = string.Format(CultureInfo.CurrentCulture, CommonHelper.GetLocalizedString("InsightProcessExitMissingDependenciesIdentifiedTitle"), processName, pid);
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

public class InsightPossibleLoaderIssue : Insight
{
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
            Debug.WriteLine(ex.Message);
        }
    }
}
