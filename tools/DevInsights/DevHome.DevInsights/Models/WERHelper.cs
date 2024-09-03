// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Threading;
using DevHome.Common.Helpers;
using DevHome.DevInsights.Helpers;
using Microsoft.Win32;
using Serilog;

namespace DevHome.DevInsights.Models;

/*
 * This class is responsible for monitoring the system for WER reports. These reports are generated when an application crashes.
 * Additionally it can be used to enable/disable local WER collection for a specific app. To learn more about local WER collection,
 * check out https://learn.microsoft.com/windows/win32/wer/collecting-user-mode-dumps
 *
 * We learn about WER events from either events in the Application event log or from crash dump files on disk.
 */

internal sealed class WERHelper : IDisposable
{
    private const string WERSubmissionQuery = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1000]])";
    private const string WERReceiveQuery = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1001]])";
    private const string DefaultDumpPath = "%LOCALAPPDATA%\\CrashDumps";
    private const string LocalWERRegistryKey = "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps";

    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WERHelper));

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly EventLogWatcher _eventLogWatcher;
    private readonly List<FileSystemWatcher> _filesystemWatchers = [];
    private readonly ObservableCollection<WERReport> _werReports = [];

    private List<string> _werLocations = [];
    private bool _isRunning;

    public ReadOnlyObservableCollection<WERReport> WERReports { get; private set; }

    public WERHelper()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        WERReports = new(_werReports);

        // Subscribe for Application events matching the processName.
        EventLogQuery subscriptionQuery = new("Application", PathType.LogName, WERSubmissionQuery);
        _eventLogWatcher = new EventLogWatcher(subscriptionQuery);
        _eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventLogEventRead);
    }

    public void Start()
    {
        if (!_isRunning)
        {
            // We get WER events from both the EventLog and from crash dump files on disk. Spin off threads
            // to look for existing crash dump files and existing event log events.
            ThreadPool.QueueUserWorkItem((o) =>
            {
                _werLocations = GetWERLocations();
                ReadLocalWERReports();
                EnableFileSystemWatchers();
            });

            ThreadPool.QueueUserWorkItem((o) =>
            {
                ReadWERReportsFromEventLog();
            });

            _eventLogWatcher.Enabled = true;

            _isRunning = true;
        }
    }

    public void Stop()
    {
        if (_isRunning)
        {
            _eventLogWatcher.Enabled = false;
            _isRunning = false;
            DisableFileSystemWatchers();
        }
    }

    public void Dispose()
    {
        _eventLogWatcher.Dispose();
        GC.SuppressFinalize(this);
    }

    // Check to see if global local WER collection is enabled
    // See https://learn.microsoft.com/windows/win32/wer/collecting-user-mode-dumps
    // for more details
    public bool IsGlobalCollectionEnabled()
    {
        var key = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, false);

        return IsCollectionEnabledForKey(key);
    }

    // See if local WER collection is enabled for a specific app
    public bool IsCollectionEnabledForApp(string appName)
    {
        var key = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, false);

        // If the local dump key doesn't exist, then app collection is disabled
        if (key is null)
        {
            return false;
        }

        var appKey = key.OpenSubKey(appName, false);

        // If the app key doesn't exist, per-app collection isn't enabled. Check the global setting
        if (appKey is null)
        {
            return IsGlobalCollectionEnabled();
        }

        return IsCollectionEnabledForKey(appKey);
    }

    private bool IsCollectionEnabledForKey(RegistryKey? key)
    {
        // If the key doesn't exist, then collection is disabled
        if (key is null)
        {
            return false;
        }

        // If the key exists, but dumpcount is set to 0, it's also disabled
        if (key.GetValue("DumpCount") is int dumpCount && dumpCount == 0)
        {
            return false;
        }

        // Collection is enabled enabled, but if we're not getting full memory dumps, so cabs may not be
        // useful. In this case, report that collection is disabled.
        var dumpType = key.GetValue("DumpType") as int?;
        if (dumpType is null || dumpType != 2)
        {
            return false;
        }

        // Otherwise it's enabled
        return true;
    }

    // This changes the registry keys necessary to allow local WER collection for a specific app
    public void EnableCollectionForApp(string appname)
    {
        RuntimeHelper.VerifyCurrentProcessRunningAsAdmin();

        var globalKey = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, true);

        if (globalKey is null)
        {
            // Need to create the key, and set the global dump collection count to 0 to prevent all apps from generating local dumps
            globalKey = Registry.LocalMachine.CreateSubKey(LocalWERRegistryKey);
            globalKey.SetValue("DumpCount", 0);
        }

        Debug.Assert(globalKey is not null, "Global key is null");

        var appKey = globalKey.CreateSubKey(appname);
        Debug.Assert(appKey is not null, "App key is null");

        // If dumpcount is set to 0, delete it to enable collection
        if (appKey.GetValue("DumpCount") is int dumpCount && dumpCount == 0)
        {
            appKey.DeleteValue("DumpCount");
        }

        // Make sure the cabs being collected are useful. Go for the full dumps instead of the mini dumps
        appKey.SetValue("DumpType", 2);

        return;
    }

    // This changes the registry keys necessary to disable local WER collection for a specific app
    public void DisableCollectionForApp(string appname)
    {
        RuntimeHelper.VerifyCurrentProcessRunningAsAdmin();

        var globalKey = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, true);

        if (globalKey is null)
        {
            // Local collection isn't enabled
            return;
        }

        var appKey = globalKey.CreateSubKey(appname);
        Debug.Assert(appKey is not null, "App key is null");

        // Set the DumpCount value to 0 to disable collection
        appKey.SetValue("DumpCount", 0);

        return;
    }

    // Callback that fires when we have a new EventLog message
    public void EventLogEventRead(object? obj, EventRecordWrittenEventArgs eventArg)
    {
        var eventRecord = eventArg.EventRecord;
        if (eventRecord != null)
        {
            if (eventRecord.Id == 1000 && eventRecord.ProviderName.Equals("Application Error", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = eventRecord.Properties[10].Value.ToString() ?? string.Empty;
                var timeGenerated = eventRecord.TimeCreated ?? DateTime.Now;
                var moduleName = eventRecord.Properties[3].Value.ToString() ?? string.Empty;
                var executable = eventRecord.Properties[0].Value.ToString() ?? string.Empty;
                var eventGuid = eventRecord.Properties[12].Value.ToString() ?? string.Empty;
                var description = eventRecord.FormatDescription();
                var pid = eventRecord.Properties[8].Value.ToString() ?? string.Empty;

                FindOrCreateWEREntryFromEventLog(filePath, timeGenerated, moduleName, executable, eventGuid, description, pid);
            }
        }
    }

    private void ReadWERReportsFromEventLog()
    {
        EventLog eventLog = new("Application");

        foreach (EventLogEntry entry in eventLog.Entries)
        {
            if (entry.InstanceId == 1000
                && entry.Source.Equals("Application Error", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = entry.ReplacementStrings[10];
                var timeGenerated = entry.TimeGenerated;
                var moduleName = entry.ReplacementStrings[3];
                var executable = entry.ReplacementStrings[0];
                var eventGuid = entry.ReplacementStrings[12];
                var description = entry.Message;
                var pid = entry.ReplacementStrings[8];

                FindOrCreateWEREntryFromEventLog(filePath, timeGenerated, moduleName, executable, eventGuid, description, pid);
            }
        }
    }

    private void FindOrCreateWEREntryFromEventLog(string filepath, DateTime timeGenerated, string moduleName, string executable, string eventGuid, string description, string processId)
    {
        int? pid = CommonHelper.ParseStringToInt(processId);

        // When adding/updating a report, we need to do it on the dispatcher thread
        _dispatcher.TryEnqueue(() =>
        {
            // Do we have an entry for this item already (created from the WER files on disk)
            var werReport = FindMatchingReport(timeGenerated, executable, pid);

            var createdReport = false;

            if (werReport is null)
            {
                werReport = new WERReport();
                createdReport = true;
                werReport.TimeStamp = timeGenerated;
                werReport.Executable = executable;
                werReport.Pid = pid ?? 0;
            }

            // Populate the report
            werReport.FilePath = filepath;
            werReport.Module = moduleName;
            werReport.EventGuid = eventGuid;
            werReport.Description = description;
            werReport.FailureBucket = GenerateFailureBucketFromEventLogDescription(description);

            // Don't add the report until it's fully populated so we have as much information as possible for our listeners
            if (createdReport)
            {
                _werReports.Add(werReport);
            }
        });
    }

    private void FindOrCreateWEREntryFromLocalDumpFile(string crashDumpFile)
    {
        var timeGenerated = File.GetCreationTime(crashDumpFile);
        FileInfo dumpFileInfo = new(crashDumpFile);

        Debug.Assert(dumpFileInfo.Exists, "Why doesn't this file exist?");

        // Only look at .dmp files
        if (dumpFileInfo.Extension != ".dmp")
        {
            return;
        }

        // The crashdumpFilename has a format of
        // executable.pid.dmp
        // so it could be
        // a.exe.40912.dmp
        // but also
        // a.b.exe.40912.dmp
        // Parse the filename starting from the back

        // Find the last dot index
        var dmpExtensionIndex = dumpFileInfo.Name.LastIndexOf('.');
        if (dmpExtensionIndex == -1)
        {
            _log.Information("Unexpected crash dump filename: " + dumpFileInfo.Name);
            return;
        }

        // Remove the .dmp. This should give us a string like a.b.exe.40912
        var filenameWithNoDmp = dumpFileInfo.Name.Substring(0, dmpExtensionIndex);

        // Find the PID
        var pidIndex = filenameWithNoDmp.LastIndexOf('.');
        if (pidIndex == -1)
        {
            _log.Information("Unexpected crash dump filename: " + crashDumpFile);
            return;
        }

        var processID = filenameWithNoDmp.Substring(pidIndex + 1);

        // Now peel off the PID. This should give us a.b.exe
        var executableFullPath = filenameWithNoDmp.Substring(0, pidIndex);

        FileInfo fileInfo = new(executableFullPath);

        string description = string.Empty;

        if (dumpFileInfo.Exists)
        {
            description = string.Format(CultureInfo.CurrentCulture, CommonHelper.GetLocalizedString("WERBasicInfo"), dumpFileInfo.Length, dumpFileInfo.CreationTime);
        }

        var converter = new Int32Converter();
        var pid = (int?)converter.ConvertFromString(processID);

        _dispatcher.TryEnqueue(() =>
        {
            // Do we have an entry for this item already (created from the event log entry)
            var werReport = FindMatchingReport(timeGenerated, fileInfo.Name, pid);

            var createdReport = false;

            if (werReport is null)
            {
                werReport = new WERReport();
                createdReport = true;
                werReport.TimeStamp = timeGenerated;
                werReport.Executable = fileInfo.Name;
                werReport.Pid = pid ?? 0;
                werReport.Description = description;
            }

            // Populate the report
            werReport.CrashDumpPath = crashDumpFile;

            // Don't add the report until it's fully populated so we have as much information as possible for our listeners
            if (createdReport)
            {
                _werReports.Add(werReport);
            }
        });

        return;
    }

    private WERReport? FindMatchingReport(DateTime timestamp, string executable, int? pid)
    {
        Debug.Assert(_dispatcher.HasThreadAccess, "This method should only be called on the dispatcher thread");
        Debug.Assert(timestamp.Kind == DateTimeKind.Local, "TimeGenerated should be in local time");
        var timestampIndex = timestamp.Ticks;

        // It's a match if the timestamp is within 2 minute of the event log entry
        var ticksWindow = new TimeSpan(0, 2, 0).Ticks;

        WERReport? werReport = null;

        // See if we can find a matching entry in the list
        foreach (var report in _werReports)
        {
            if (report.Executable.Equals(executable, StringComparison.OrdinalIgnoreCase) && report.Pid == pid)
            {
                // See if the timestamps are "close enough"
                Debug.Assert(report.TimeStamp.Kind == DateTimeKind.Local, "TimeGenerated should be in local time");
                var ticksDiff = Math.Abs(report.TimeStamp.Ticks - timestampIndex);

                if (ticksDiff < ticksWindow)
                {
                    werReport = report;
                    break;
                }
            }
        }

        return werReport;
    }

    private string GenerateFailureBucketFromEventLogDescription(string description)
    {
        /* The description can look like this

        Faulting application name: DevHome.PI.exe, version: 1.0.0.0, time stamp: 0x66470000
        Faulting module name: KERNELBASE.dll, version: 10.0.22621.3810, time stamp: 0x10210ca8
        Exception code: 0xe0434352
        Fault offset: 0x000000000005f20c
        Faulting process id: 0x0xa078
        Faulting application start time: 0x0x1dad175bd05dea9
        Faulting application path: E:\devhome\src\bin\x64\Debug\net8.0-windows10.0.22621.0\AppX\DevHome.PI.exe
        Faulting module path: C:\WINDOWS\System32\KERNELBASE.dll
        Report Id: 7a4cd0a8-f65b-4f27-b250-cc5bd57e39d6
        Faulting package full name: Microsoft.Windows.DevHome.Dev_0.0.0.0_x64__8wekyb3d8bbwe
        Faulting package-relative application ID: Devhome.PI

        Let's create a placeholder failure bucket based on the module name, offset, and exception code. In the above example,
        we'll generate a bucket "KERNELBASE.dll+0x000000000005f20c 0xe0434352"
        */

        var lines = description.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? moduleName = null;
        string? exceptionCode = null;
        string? faultOffset = null;

        foreach (var line in lines)
        {
            if (line.Contains("Fault offset:"))
            {
                faultOffset = line.Substring(line.IndexOf(':') + 1).Trim();
            }
            else if (line.Contains("Exception code:"))
            {
                exceptionCode = line.Substring(line.IndexOf(':') + 1).Trim();
            }
            else if (line.Contains("Faulting module name:"))
            {
                var startIndex = line.IndexOf(':') + 1;
                var endIndex = line.IndexOf(',') - 1;

                moduleName = line.Substring(startIndex, endIndex - startIndex + 1).Trim();
            }
        }

        if (moduleName is not null && exceptionCode is not null && faultOffset is not null)
        {
            return $"{moduleName}+{faultOffset} {exceptionCode}";
        }

        return string.Empty;
    }

    private void ReadLocalWERReports()
    {
        foreach (var dumpLocation in _werLocations)
        {
            try
            {
                // Enumerate all of the existing dump files in this location
                foreach (var dumpFile in Directory.EnumerateFiles(dumpLocation, "*.dmp"))
                {
                    FindOrCreateWEREntryFromLocalDumpFile(dumpFile);
                }
            }
            catch
            {
                _log.Error("Error enumerating directory " + dumpLocation);
            }
        }
    }

    // Generate a list of all of the locations on disk where local WER dumps are stored
    private List<string> GetWERLocations()
    {
        var list = new List<string>();

        var key = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, false);

        if (key is not null)
        {
            var globaldumppath = GetDumpPath(key);

            Debug.Assert(globaldumppath is not null, "Global dump path is not set");
            list.Add(globaldumppath);

            var subKeys = key.GetSubKeyNames();
            foreach (var subkey in subKeys)
            {
                var dumpPath = GetDumpPath(key.OpenSubKey(subkey));

                if (dumpPath is not null)
                {
                    // If this item isn't in the list, add it.
                    if (!list.Contains(dumpPath))
                    {
                        list.Add(dumpPath);
                    }
                }
            }
        }

        return list;
    }

    private string? GetDumpPath(RegistryKey? key)
    {
        if (key is not null)
        {
            if (key.GetValue("DumpFolder") is not string dumpFolder)
            {
                // If a dumppath isn't explicitly set, then use the system's default dump path
                dumpFolder = DefaultDumpPath;
            }

            return Environment.ExpandEnvironmentVariables(dumpFolder);
        }

        return null;
    }

    // Enable watchers to catch new WER dumps as they are generated
    private void EnableFileSystemWatchers()
    {
        _filesystemWatchers.Clear();

        foreach (var path in _werLocations)
        {
            // If this directory exists, monitor it for new files
            if (Directory.Exists(path))
            {
                var watcher = new FileSystemWatcher(path);
                watcher.Created += (sender, e) =>
                {
                    _log.Information($"New dump file: {e.FullPath}");
                    FindOrCreateWEREntryFromLocalDumpFile(e.FullPath);
                };

                watcher.EnableRaisingEvents = true;
                _filesystemWatchers.Add(watcher);
            }
        }
    }

    private void DisableFileSystemWatchers()
    {
        _filesystemWatchers.Clear();
    }
}
