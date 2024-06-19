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
            else if (eventRecord.Id == 1001 && eventRecord.ProviderName.Equals("Windows Error Reporting", StringComparison.OrdinalIgnoreCase))
            {
                // See if we've already put this into our Collection.
                for (var i = 0; i < _watsonReports.Count; i++)
                {
                    var existingReport = _watsonReports[i];
                    if (existingReport.EventGuid.Equals(eventRecord.Properties[19].Value.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        existingReport.WatsonLog = eventRecord.FormatDescription();
                        try
                        {
                            // List files available in the archive.
                            var directoryPath = eventRecord.Properties[16].Value.ToString();
                            if (Directory.Exists(directoryPath))
                            {
                                IEnumerable<string> files = Directory.EnumerateFiles(directoryPath);
                                foreach (var file in files)
                                {
                                    existingReport.WatsonReportFile = File.ReadAllText(file);
                                }
                            }
                        }
                        catch
                        {
                        }

                        break;
                    }
                }
            }
        }
    }

    private void ReadWatsonReportsFromEventLog()
    {
        Dictionary<string, WatsonReport> partialReports = [];
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
                var report = new WatsonReport(filePath, timeGenerated, moduleName, executable, eventGuid, description);
                partialReports.Add(entry.ReplacementStrings[12], report);
            }
            else if (entry.InstanceId == 1001 && entry.Source.Equals("Windows Error Reporting", StringComparison.OrdinalIgnoreCase))
            {
                // See if we've already put this into our Dictionary.
                if (partialReports.TryGetValue(entry.ReplacementStrings[19], out WatsonReport? report))
                {
                    report.WatsonLog = entry.Message;

                    try
                    {
                        // List files available in the archive.
                        if (Directory.Exists(entry.ReplacementStrings[16]))
                        {
                            var files = Directory.EnumerateFiles(entry.ReplacementStrings[16]);
                            foreach (var file in files)
                            {
                                report.WatsonReportFile = File.ReadAllText(file);
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        // We've gathered all the data from this Watson report. Publish it.
                        _watsonReports.Add(report);
                        partialReports.Remove(entry.ReplacementStrings[19]);
                    }
                }
            }
        }

        // For the remainer of the partial Watson events, just publish them. We won't get more info for them.
        foreach (var report in partialReports.Values)
        {
            _watsonReports.Add(report);
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
