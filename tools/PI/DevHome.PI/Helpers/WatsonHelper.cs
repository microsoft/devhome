// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Threading;
using DevHome.Common.Helpers;
using DevHome.PI.Models;
using Microsoft.Win32;
using Serilog;

namespace DevHome.PI.Helpers;

internal sealed class WatsonHelper : IDisposable
{
    private const string WatsonSubmissionQuery = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1000]])";
    private const string WatsonReceiveQuery = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1001]])";
    private const string DefaultDumpPath = "%LOCALAPPDATA%\\CrashDumps";
    private const string LocalWatsonRegistryKey = "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps";

    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WatsonHelper));

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly EventLogWatcher _eventLogWatcher;
    private readonly List<FileSystemWatcher> _filesystemWatchers = [];
    private readonly ObservableCollection<WatsonReport> _watsonReports = [];
    public static readonly WatsonHelper Instance = new();

    private List<string> _watsonLocations = [];
    private bool _isRunning;

    public ReadOnlyObservableCollection<WatsonReport> WatsonReports { get; private set; }

    public WatsonHelper()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        WatsonReports = new(_watsonReports);

        // Subscribe for Application events matching the processName.
        EventLogQuery subscriptionQuery = new("Application", PathType.LogName, WatsonSubmissionQuery);
        _eventLogWatcher = new EventLogWatcher(subscriptionQuery);
        _eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventLogEventRead);
    }

    public void Start()
    {
        if (!_isRunning)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                _watsonLocations = GetWatsonLocations();
                ReadLocalWatsonReports();
                EnableFileSystemWatchers();
            });

            ThreadPool.QueueUserWorkItem((o) =>
            {
                ReadWatsonReportsFromEventLog();
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
        RegistryKey? key = Registry.LocalMachine.OpenSubKey(LocalWatsonRegistryKey, false);

        return IsCollectionEnabledForKey(key);
    }

    // See if local WER collection is enabled for a specific app
    public bool IsCollectionEnabledForApp(string appName)
    {
        RegistryKey? key = Registry.LocalMachine.OpenSubKey(LocalWatsonRegistryKey, false);

        // If the local dump key doesn't exist, then app collection is disabled
        if (key is null)
        {
            return false;
        }

        RegistryKey? appKey = key.OpenSubKey(appName, false);

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

        RegistryKey? globalKey = Registry.LocalMachine.OpenSubKey(LocalWatsonRegistryKey, true);

        if (globalKey is null)
        {
            // Need to create the key, and set the global dump collection count to 0 to prevent all apps from generating local dumps
            globalKey = Registry.LocalMachine.CreateSubKey(LocalWatsonRegistryKey);
            globalKey.SetValue("DumpCount", 0);
        }

        Debug.Assert(globalKey is not null, "Global key is null");

        RegistryKey? appKey = globalKey.CreateSubKey(appname);
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

        RegistryKey? globalKey = Registry.LocalMachine.OpenSubKey(LocalWatsonRegistryKey, true);

        if (globalKey is null)
        {
            // Local collection isn't enabled
            return;
        }

        RegistryKey? appKey = globalKey.CreateSubKey(appname);
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

                FindOrCreateWatsonEntryFromEventLog(filePath, timeGenerated, moduleName, executable, eventGuid, description, pid);
            }
        }
    }

    private void ReadWatsonReportsFromEventLog()
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

                FindOrCreateWatsonEntryFromEventLog(filePath, timeGenerated, moduleName, executable, eventGuid, description, pid);
            }
        }
    }

    private void FindOrCreateWatsonEntryFromEventLog(string filepath, DateTime timeGenerated, string moduleName, string executable, string eventGuid, string description, string processId)
    {
        var converter = new Int32Converter();
        int? pid = (int?)converter.ConvertFromString(processId);

        lock (_watsonReports)
        {
            // Do we have an entry for this item already (created from the Watson files on disk)
            WatsonReport? watsonReport = FindMatchingReport(timeGenerated, executable, pid);

            // When adding/updating a report, we need to do it on the dispatcher thread
            _dispatcher.TryEnqueue(() =>
            {
                bool createdReport = false;

                if (watsonReport is null)
                {
                    watsonReport = new WatsonReport();
                    createdReport = true;
                    watsonReport.TimeStamp = timeGenerated;
                    watsonReport.Executable = executable;
                    watsonReport.Pid = pid ?? 0;
                }

                // Populate the report
                watsonReport.FilePath = filepath;
                watsonReport.Module = moduleName;
                watsonReport.EventGuid = eventGuid;
                watsonReport.Description = description;
                watsonReport.FailureBucket = GenerateFailureBucketFromEventLogDescription(description);

                // Don't add the report until it's fully populated so we have as much information as possible for our listeners
                if (createdReport)
                {
                    _watsonReports.Add(watsonReport);
                }
            });
        }
    }

    private void FindOrCreateWatsonEntryFromLocalDumpFile(string crashDumpFile)
    {
        var timeGenerated = File.GetCreationTime(crashDumpFile);

        // The crashdumpFilename has a format of
        // executable.pid.dmp
        // so it could be
        // a.exe.40912.dmp
        // but also
        // a.b.exe.40912.dmp
        // Parse the filename starting from the back

        // Find the last dot index
        int dmpExtensionIndex = crashDumpFile.LastIndexOf('.');
        if (dmpExtensionIndex == -1)
        {
            _log.Information("Unexpected crash dump filename: " + crashDumpFile);
            return;
        }

        // Remove the .dmp. This should give us a string like a.b.exe.40912
        string filenameWithNoDmp = crashDumpFile.Substring(0, dmpExtensionIndex);

        // Find the PID
        int pidIndex = filenameWithNoDmp.LastIndexOf('.');
        if (pidIndex == -1)
        {
            _log.Information("Unexpected crash dump filename: " + crashDumpFile);
            return;
        }

        string processID = filenameWithNoDmp.Substring(pidIndex + 1);

        // Now peel off the PID. This should give us a.b.exe
        string executableFullPath = filenameWithNoDmp.Substring(0, pidIndex);

        FileInfo fileInfo = new(executableFullPath);

        var converter = new Int32Converter();
        int? pid = (int?)converter.ConvertFromString(processID);

        lock (_watsonReports)
        {
            // Do we have an entry for this item already (created from the event log entry)
            WatsonReport? watsonReport = FindMatchingReport(timeGenerated, fileInfo.Name, pid);

            _dispatcher.TryEnqueue(() =>
            {
                bool createdReport = false;

                if (watsonReport is null)
                {
                    watsonReport = new WatsonReport();
                    createdReport = true;
                    watsonReport.TimeStamp = timeGenerated;
                    watsonReport.Executable = fileInfo.Name;
                    watsonReport.Pid = pid ?? 0;
                }

                // Populate the report
                watsonReport.CrashDumpPath = crashDumpFile;

                // Don't add the report until it's fully populated so we have as much information as possible for our listeners
                if (createdReport)
                {
                    _watsonReports.Add(watsonReport);
                }
            });
        }

        return;
    }

    private WatsonReport? FindMatchingReport(DateTime timestamp, string executable, int? pid)
    {
        Debug.Assert(timestamp.Kind == DateTimeKind.Local, "TimeGenerated should be in local time");
        long timestampIndex = timestamp.Ticks;

        // It's a match if the timestamp is within 1 minute of the event log entry
        long ticksWindow = new TimeSpan(0, 1, 0).Ticks;

        WatsonReport? watsonReport = null;

        // See if we can find a matching entry in the list
        foreach (var report in _watsonReports)
        {
            if (report.Executable == executable && report.Pid == pid)
            {
                // See if the timestamps are "close enough"
                Debug.Assert(report.TimeStamp.Kind == DateTimeKind.Local, "TimeGenerated should be in local time");
                long ticksDiff = Math.Abs(report.TimeStamp.Ticks - timestampIndex);

                if (ticksDiff < ticksWindow)
                {
                    watsonReport = report;
                    break;
                }
            }
        }

        return watsonReport;
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

        string[] lines = description.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? moduleName = null;
        string? exceptionCode = null;
        string? faultOffset = null;

        foreach (string line in lines)
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
                int startIndex = line.IndexOf(':') + 1;
                int endIndex = line.IndexOf(',') - 1;

                moduleName = line.Substring(startIndex, endIndex - startIndex + 1).Trim();
            }
        }

        if (moduleName is not null && exceptionCode is not null && faultOffset is not null)
        {
            return $"{moduleName}+{faultOffset} {exceptionCode}";
        }

        return string.Empty;
    }

    private void ReadLocalWatsonReports()
    {
        foreach (var dumpLocation in _watsonLocations)
        {
            try
            {
                // Enumerate all of the existing dump files in this location
                foreach (var dumpFile in System.IO.Directory.EnumerateFiles(dumpLocation, "*.dmp"))
                {
                    FindOrCreateWatsonEntryFromLocalDumpFile(dumpFile);
                }
            }
            catch
            {
                _log.Error("Error enumerating directory " + dumpLocation);
            }
        }
    }

    // Generate a list of all of the locations on disk where local WER dumps are stored
    private List<string> GetWatsonLocations()
    {
        List<string> list = new List<string>();

        RegistryKey? key = Registry.LocalMachine.OpenSubKey(LocalWatsonRegistryKey, false);

        if (key is not null)
        {
            string? globaldumppath = GetDumpPath(key);

            Debug.Assert(globaldumppath is not null, "Global dump path is not set");
            list.Add(globaldumppath);

            string[] subKeys = key.GetSubKeyNames();
            foreach (var subkey in subKeys)
            {
                string? dumpPath = GetDumpPath(key.OpenSubKey(subkey));

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

        foreach (var path in _watsonLocations)
        {
            // If this directory exists, monitor it for new files
            if (Directory.Exists(path))
            {
                var watcher = new FileSystemWatcher(path);
                watcher.Created += (sender, e) =>
                {
                    _log.Information($"New dump file: {e.FullPath}");
                    FindOrCreateWatsonEntryFromLocalDumpFile(e.FullPath);
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
