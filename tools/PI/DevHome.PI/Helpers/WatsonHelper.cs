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
using System.Linq;
using System.Threading;
using DevHome.PI.Models;
using TraceReloggerLib;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace DevHome.PI.Helpers;

internal sealed class WatsonHelper : IDisposable
{
    private const string WatsonQueryPart1 = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1000]])";

    private readonly EventLogWatcher _eventLogWatcher;
    private readonly ObservableCollection<WatsonReport> _watsonReports = [];
    public static readonly WatsonHelper Instance = new();

    public ReadOnlyObservableCollection<WatsonReport> WatsonReports { get; private set; }

    private bool _isRunning;

    public WatsonHelper()
    {
        WatsonReports = new(_watsonReports);

        // Subscribe for Application events matching the processName.
        EventLogQuery subscriptionQuery = new("Application", PathType.LogName, WatsonQueryPart1);
        _eventLogWatcher = new EventLogWatcher(subscriptionQuery);
        _eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventLogEventRead);
    }

    public void Start()
    {
        if (!_isRunning)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                ReadWatsonReportsFromEventLog();
            });

            ThreadPool.QueueUserWorkItem((o) =>
            {
                // ReadLocalWatsonReports();
            });

            _eventLogWatcher.Enabled = true;

            _isRunning = true;
        }
    }

    public void Stop()
    {
        if (!_isRunning)
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
                var report = new WatsonReport(filePath, timeGenerated, moduleName, executable, eventGuid, description);
                _watsonReports.Add(report);
            }
        }
    }

    private void ReadWatsonReportsFromEventLog()
    {
        EventLog eventLog = new("Application");
        var converter = new Int32Converter();

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

                var report = new WatsonReport(filePath, timeGenerated, moduleName, executable, eventGuid, description);

                // Does a local crash dump exist for this item?
                // Crash dump files are of the form:
                // MfcFormApp.exe.40912.dmp
                // Build the possible path for them
                if (converter.IsValid(entry.ReplacementStrings[8]))
                {
                    var pid = (int?)converter.ConvertFromString(entry.ReplacementStrings[8]);
                    Debug.Assert(pid != null, "Why did the conversion fail?");
                    var crashDumpFilename = executable + "." + pid?.ToString(CultureInfo.InvariantCulture) + ".dmp";

                    string crashDumpPath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty, "CrashDumps", crashDumpFilename);

                    if (File.Exists(crashDumpPath))
                    {
                        report.CrashDumpPath = crashDumpPath;
                    }
                }

                _watsonReports.Add(report);
            }
        }
    }

    private void ReadLocalWatsonReports()
    {
        string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty;
        string crashDumpsPath = Path.Combine(localAppData, "CrashDumps");

        var files = Directory.EnumerateFiles(crashDumpsPath, "*.dmp");

        foreach (var fileName in files)
        {
            var file = new FileInfo(fileName);

            string[] elements = file.Name.Split('.');

            var report = new WatsonReport(file.FullName, file.CreationTime, string.Empty, elements[0] + "." + elements[1], string.Empty, string.Empty);
            _watsonReports.Add(report);
        }
    }
}
