// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using DevHome.DevDiagnostics.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Serilog;

namespace DevHome.DevDiagnostics.Helpers;

internal sealed class ETWHelper : IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ETWHelper));
    private readonly Process _targetProcess;
    private readonly ObservableCollection<WinLogsEntry> _output;

    private static readonly List<string> _providerList = ["1AFF6089-E863-4D36-BDFD-3581F07440BE" /*COM Tracelog*/];
    private TraceEventSession? _session;

    // From: https://learn.microsoft.com/windows-server/identity/ad-ds/manage/understand-security-identifiers
    private const string PerformanceLogUsersSid = "S-1-5-32-559";

    public const string AddUserToPerformanceLogUsersSid = @"
        Add-LocalGroupMember -Sid S-1-5-32-559 -Member ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)
        # Check if the last command succeeded
        if ($?)
        {
            # User added to the Performance Log Users group.
            exit 0
        }
        exit 1
        ";

    public ETWHelper(Process targetProcess, ObservableCollection<WinLogsEntry> output)
    {
        _targetProcess = targetProcess;
        _targetProcess.Exited += TargetProcess_Exited;
        _output = output;
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
            _session = TraceEventSession.GetActiveSession(sessionName);
            if (_session is not null)
            {
                _session.Stop();
                _session.Dispose();
            }

            try
            {
                using (_session = new TraceEventSession(sessionName))
                {
                    // Filter the provider events based on processId
                    var providerOptions = new TraceEventProviderOptions { ProcessIDFilter = [_targetProcess.Id] };
                    foreach (var provider in _providerList)
                    {
                        _session.EnableProvider(provider, TraceEventLevel.Always, options: providerOptions);
                    }

                    _session.Source.Dynamic.All += EventsHandler;
                    _session.Source.UnhandledEvents += UnHandledEventsHandler;
                    _session.Source.Process();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Stop();
                WinLogsEntry entry = new(DateTime.Now, WinLogCategory.Error, ex.Message, WinLogsHelper.EtwLogsName);
                _output.Add(entry);
            }
        }
    }

    public void Stop()
    {
        _session?.Stop();
    }

    public void Dispose()
    {
        _session?.Dispose();
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
        if (processId != _targetProcess.Id)
        {
            return;
        }

        var category = WinLogsHelper.ConvertTraceEventLevelToWinLogCategory(level);
        var entry = new WinLogsEntry(timeStamp, category, message, WinLogsHelper.EtwLogsName);
        _output.Add(entry);
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
        startInfo.FileName = $"powershell.exe";

        // Add the user to the Performance Log Users group
        startInfo.Arguments = $"-ExecutionPolicy Bypass -Command \"{AddUserToPerformanceLogUsersSid}\"";
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
