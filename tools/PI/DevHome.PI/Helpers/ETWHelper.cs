// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using DevHome.PI.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Serilog;

namespace DevHome.PI.Helpers;

internal sealed class ETWHelper : IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ETWHelper));
    private readonly Process targetProcess;
    private readonly ObservableCollection<WinLogsEntry> output;

    // TODO Add providers
    private static readonly List<string> ProviderList = ["1AFF6089-E863-4D36-BDFD-3581F07440BE" /*COM Tracelog*/];
    private TraceEventSession? session;

    // From: https://learn.microsoft.com/windows-server/identity/ad-ds/manage/understand-security-identifiers
    private const string PerformanceLogUsersSid = "S-1-5-32-559";

    public ETWHelper(Process targetProcess, ObservableCollection<WinLogsEntry> output)
    {
        this.targetProcess = targetProcess;
        this.targetProcess.Exited += TargetProcess_Exited;
        this.output = output;
    }

    public void Start()
    {
        var isUserInPerformanceLogUsersGroup = IsUserInPerformanceLogUsersGroup();

        if (!isUserInPerformanceLogUsersGroup)
        {
            isUserInPerformanceLogUsersGroup = TryAddUserToPerformanceLogUsersGroup();
        }

        if (isUserInPerformanceLogUsersGroup)
        {
            var sessionName = "DevHomePITrace" + Process.GetCurrentProcess().SessionId;

            // Stop and dispose any existing session
            session = TraceEventSession.GetActiveSession(sessionName);
            if (session is not null)
            {
                session.Stop();
                session.Dispose();
            }

            using (session = new TraceEventSession(sessionName))
            {
                // Filter the provider events based on processId
                var providerOptions = new TraceEventProviderOptions { ProcessIDFilter = [targetProcess.Id] };
                foreach (var provider in ProviderList)
                {
                    session.EnableProvider(provider, TraceEventLevel.Always, options: providerOptions);
                }

                session.Source.Dynamic.All += EventsHandler;
                session.Source.UnhandledEvents += UnHandledEventsHandler;
                session.Source.Process();
            }
        }
    }

    public void Stop()
    {
        session?.Stop();
    }

    public void Dispose()
    {
        session?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void EventsHandler(TraceEvent traceEvent)
    {
        ETWEventHandler(traceEvent.ProcessID, traceEvent.TimeStamp, traceEvent.Level, traceEvent.ToString(CultureInfo.CurrentCulture));
    }

    private void UnHandledEventsHandler(TraceEvent traceEvent)
    {
        var errorMessage = CommonHelper.GetLocalizedString("UnhandledTraceEventErrorMessage", traceEvent.Dump());
        ETWEventHandler(traceEvent.ProcessID, traceEvent.TimeStamp, traceEvent.Level, errorMessage);
    }

    private void ETWEventHandler(int processId, DateTime timeStamp, TraceEventLevel level, string message)
    {
        if (processId != targetProcess.Id)
        {
            return;
        }

        var category = WinLogsHelper.ConvertTraceEventLevelToWinLogCategory(level);
        var entry = new WinLogsEntry(timeStamp, category, message, WinLogsHelper.EtwLogsName);
        output.Add(entry);
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }

    public static bool IsUserInPerformanceLogUsersGroup()
    {
        WindowsIdentity processUserIdentity = WindowsIdentity.GetCurrent();
        var isPerformanceLogSidFound = processUserIdentity.Groups?.Any(sid => sid.Value == PerformanceLogUsersSid);
        return isPerformanceLogSidFound ?? false;
    }

    public static bool TryAddUserToPerformanceLogUsersGroup()
    {
        WindowsIdentity processUserIdentity = WindowsIdentity.GetCurrent();
        var userName = processUserIdentity.Name;
        if (userName is null)
        {
            _log.Error("Unable to get the current user name");
            return false;
        }

        var startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = Environment.SystemDirectory + "\\net.exe";

        // Add the user to the Performance Log Users group
        startInfo.Arguments = $"localgroup \"Performance Log Users\" {userName} /add";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;

        // Since a UAC prompt will be shown, we need to wait for the process to exit
        // This can also be cancelled by the user which will result in an exception
        try
        {
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return true;
            }
            else
            {
                _log.Error("Unable to add the user to the Performance Log Users group");
                return false;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "UAC to add the user to the Performance Log Users group was denied");
        }

        return false;
    }
}
