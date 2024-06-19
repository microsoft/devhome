// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private const string WatsonQueryPart2 = "(*[System[Provider[@Name=\"Windows Error Reporting\"]]] and *[System[EventID=1001]])";

    private readonly EventLogWatcher _eventLogWatcher;
    private readonly ObservableCollection<WatsonReport> _watsonReports = [];
    public static readonly WatsonHelper Instance = new();

    public ReadOnlyObservableCollection<WatsonReport> WatsonReports { get; private set; }

    private bool _isRunning;

    public WatsonHelper()
    {
        WatsonReports = new(_watsonReports);

        // Subscribe for Application events matching the processName.
        var filterQuery = string.Format(CultureInfo.CurrentCulture, "{0} or {1}", WatsonQueryPart1, WatsonQueryPart2);
        EventLogQuery subscriptionQuery = new("Application", PathType.LogName, filterQuery);
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
                ReadLocalWatsonReports();
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

                // Does a local crash dump exist for this item?
                var crashDumpFilename = moduleName + "123" + "dmp";
                string crashDumpPath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty, "CrashDumps", crashDumpFilename);

                if (!File.Exists(crashDumpPath))
                {
                    crashDumpPath = string.Empty;
                }

                var report = new WatsonReport(filePath, timeGenerated, moduleName, executable, eventGuid, description);

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
