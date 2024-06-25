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
using DevHome.PI.Models;
using Microsoft.Win32;

namespace DevHome.PI.Helpers;

internal sealed class WatsonHelper : IDisposable
{
    private const string WatsonSubmissionQuery = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1000]])";
    private const string WatsonReceiveQuery = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1001]])";
    private const string DefaultDumpPath = "%LOCALAPPDATA%\\CrashDumps";
    private const string LocalWatsonRegistryKey = "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps";

    private readonly EventLogWatcher _eventLogWatcher;
    private readonly List<FileSystemWatcher> _filesystemWatchers = [];
    private readonly ObservableCollection<WatsonReport> _watsonReports = [];
    public static readonly WatsonHelper Instance = new();

    // Key is the filename, value is the full path to the dump file
    private List<string> _watsonLocations = [];

    private bool _isRunning;

    public ReadOnlyObservableCollection<WatsonReport> WatsonReports { get; private set; }

    public WatsonHelper()
    {
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
        }
    }

    public void Dispose()
    {
        _eventLogWatcher.Dispose();
        GC.SuppressFinalize(this);
    }

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

                FindOrCreateWatsonEntry(filePath, timeGenerated, moduleName, executable, eventGuid, description, pid);
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

                FindOrCreateWatsonEntry(filePath, timeGenerated, moduleName, executable, eventGuid, description, pid);
            }
        }
    }

    private void FindOrCreateWatsonEntry(string filepath, DateTime timeGenerated, string moduleName, string executable, string eventGuid, string description, string processId)
    {
        var converter = new Int32Converter();
        int? pid = (int?)converter.ConvertFromString(processId);

        lock (_watsonReports)
        {
            // Do we have an entry for this item already (created from the Watson files on disk)
            WatsonReport? watsonReport = FindMatchingReport(timeGenerated, executable, pid);

            if (watsonReport is null)
            {
                watsonReport = new WatsonReport();
                watsonReport.TimeStamp = timeGenerated;
                watsonReport.Executable = executable;
                watsonReport.Pid = pid ?? 0;
                _watsonReports.Add(watsonReport);
            }

            // Populate the report
            watsonReport.FilePath = filepath;
            watsonReport.Module = moduleName;
            watsonReport.EventGuid = eventGuid;
            watsonReport.Description = description;
        }
    }

    private void FindOrCreateWatsonEntry(string crashDumpFile)
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
            Trace.WriteLine("Unexpected crash dump filename: " + crashDumpFile);
            return;
        }

        // Remove the .dmp. This should give us a string like a.b.exe.40912
        string filenameWithNoDmp = crashDumpFile.Substring(0, dmpExtensionIndex);

        // Find the PID
        int pidIndex = filenameWithNoDmp.LastIndexOf('.');
        if (pidIndex == -1)
        {
            Trace.WriteLine("Unexpected crash dump filename: " + crashDumpFile);
            return;
        }

        string processID = filenameWithNoDmp.Substring(pidIndex + 1);

        // Now peel off the PID. This should give us a.b.exe
        string executable = filenameWithNoDmp.Substring(0, pidIndex);

        var converter = new Int32Converter();
        int? pid = (int?)converter.ConvertFromString(processID);

        lock (_watsonReports)
        {
            // Do we have an entry for this item already (created from the Watson files on disk)
            WatsonReport? watsonReport = FindMatchingReport(timeGenerated, executable, pid);

            if (watsonReport is null)
            {
                watsonReport = new WatsonReport();
                watsonReport.TimeStamp = timeGenerated;
                watsonReport.Executable = executable;
                watsonReport.Pid = pid ?? 0;
                _watsonReports.Add(watsonReport);
            }

            // Populate the report
            watsonReport.CrashDumpPath = crashDumpFile;
        }

        return;
    }

    private WatsonReport? FindMatchingReport(DateTime timestamp, string executable, int? pid)
    {
        Debug.Assert(timestamp.Kind == DateTimeKind.Local, "TimeGenerated is not in local time");
        long timestampIndex = timestamp.Ticks;

        // It's a match if the timestamp is within 2 minutes of the event log entry
        long ticksWindow = new TimeSpan(0, 2, 0).Ticks;

        WatsonReport? watsonReport = null;

        // See if we can find a matching entry in the list
        foreach (var report in _watsonReports)
        {
            if (report.Executable == executable && report.Pid == pid)
            {
                // See if the timestamps are "close enough"
                Debug.Assert(report.TimeStamp.Kind == DateTimeKind.Local, "TimeGenerated is not in local time");
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

    private void ReadLocalWatsonReports()
    {
        foreach (var dumpLocation in _watsonLocations)
        {
            try
            {
                // Enumerate all of the existing dump files in this location
                foreach (var dumpFile in System.IO.Directory.EnumerateFiles(dumpLocation, "*.dmp"))
                {
                    FindOrCreateWatsonEntry(dumpFile);
                }
            }
            catch
            {
                Trace.WriteLine("Error enumerating directory " + dumpLocation);
            }
        }
    }

    private List<string> GetWatsonLocations()
    {
        List<string> list = new List<string>();

        RegistryKey? key = Registry.LocalMachine.OpenSubKey(LocalWatsonRegistryKey, false);

        if (key is not null)
        {
            string? globaldumppath = GetDumpPath(key);

            Debug.Assert(globaldumppath is not null, "Global dump path is not set");
            list.Add(globaldumppath);
            AddFileSystemMonitor(globaldumppath);

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
                        AddFileSystemMonitor(dumpPath);
                    }
                }
            }
        }

        return list;
    }

    private void AddFileSystemMonitor(string path)
    {
        // If this directory exists, monitor it for new files
        if (Directory.Exists(path))
        {
            var watcher = new FileSystemWatcher(path);
            watcher.Created += (sender, e) =>
            {
                Trace.WriteLine($"New dump file: {e.FullPath}");
                FindOrCreateWatsonEntry(e.FullPath);
            };

            watcher.EnableRaisingEvents = true;
            _filesystemWatchers.Add(watcher);
        }
    }

    private string? GetDumpPath(RegistryKey? key)
    {
        if (key is not null)
        {
            if (key.GetValue("DumpFolder") is not string dumpFolder)
            {
                dumpFolder = DefaultDumpPath;
            }

            return Environment.ExpandEnvironmentVariables(dumpFolder);
        }

        return null;
    }
}
