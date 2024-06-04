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
using DevHome.PI.Models;

namespace DevHome.PI.Helpers;

internal sealed class WatsonHelper : IDisposable
{
    private const string WatsonQueryPart1 = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1000]])";
    private const string WatsonQueryPart2 = "(*[System[Provider[@Name=\"Windows Error Reporting\"]]] and *[System[EventID=1001]])";

    private readonly Process targetProcess;
    private readonly EventLogWatcher? eventLogWatcher;
    private readonly ObservableCollection<WatsonReport>? watsonOutput;
    private readonly ObservableCollection<WinLogsEntry>? winLogsPageOutput;

    public WatsonHelper(Process targetProcess, ObservableCollection<WatsonReport>? watsonOutput, ObservableCollection<WinLogsEntry>? winLogsPageOutput)
    {
        this.targetProcess = targetProcess;
        this.targetProcess.Exited += TargetProcess_Exited;
        this.watsonOutput = watsonOutput;
        this.winLogsPageOutput = winLogsPageOutput;

        try
        {
            // Subscribe for Application events matching the processName.
            var filterQuery = string.Format(CultureInfo.CurrentCulture, "{0} or {1}", WatsonQueryPart1, WatsonQueryPart2);
            EventLogQuery subscriptionQuery = new("Application", PathType.LogName, filterQuery);
            eventLogWatcher = new EventLogWatcher(subscriptionQuery);
            eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventLogEventRead);
        }
        catch (EventLogReadingException)
        {
            var message = CommonHelper.GetLocalizedString("WatsonStartErrorMessage");
            WinLogsEntry entry = new(DateTime.Now, WinLogCategory.Error, message, WinLogsHelper.WatsonName);
            winLogsPageOutput?.Add(entry);
        }
    }

    public void Start()
    {
        if (eventLogWatcher is not null)
        {
            eventLogWatcher.Enabled = true;
        }
    }

    public void Stop()
    {
        if (eventLogWatcher is not null)
        {
            eventLogWatcher.Enabled = false;
        }
    }

    public void Dispose()
    {
        if (eventLogWatcher is not null)
        {
            eventLogWatcher.Dispose();
        }

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
                if (filePath.Contains(targetProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    var timeGenerated = eventRecord.TimeCreated ?? DateTime.Now;
                    var moduleName = eventRecord.Properties[3].Value.ToString() ?? string.Empty;
                    var executable = eventRecord.Properties[0].Value.ToString() ?? string.Empty;
                    var eventGuid = eventRecord.Properties[12].Value.ToString() ?? string.Empty;
                    var report = new WatsonReport(timeGenerated, moduleName, executable, eventGuid);
                    watsonOutput?.Add(report);

                    WinLogsEntry entry = new(timeGenerated, WinLogCategory.Error, eventRecord.FormatDescription(), WinLogsHelper.WatsonName);
                    winLogsPageOutput?.Add(entry);
                }
            }
            else if (eventRecord.Id == 1001 && eventRecord.ProviderName.Equals("Windows Error Reporting", StringComparison.OrdinalIgnoreCase))
            {
                // See if we've already put this into our Collection.
                for (var i = 0; i < watsonOutput?.Count; i++)
                {
                    var existingReport = watsonOutput[i];
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

    public List<WatsonReport> GetWatsonReports()
    {
        Dictionary<string, WatsonReport> reports = [];
        EventLog eventLog = new("Application");
        var targetProcessName = targetProcess.ProcessName;

        foreach (EventLogEntry entry in eventLog.Entries)
        {
            if (entry.InstanceId == 1000
                && entry.Source.Equals("Application Error", StringComparison.OrdinalIgnoreCase)
                && entry.ReplacementStrings[10].Contains(targetProcessName, StringComparison.OrdinalIgnoreCase))
            {
                var timeGenerated = entry.TimeGenerated;
                var moduleName = entry.ReplacementStrings[3];
                var executable = entry.ReplacementStrings[0];
                var eventGuid = entry.ReplacementStrings[12];
                var report = new WatsonReport(timeGenerated, moduleName, executable, eventGuid);
                reports.Add(entry.ReplacementStrings[12], report);
            }
            else if (entry.InstanceId == 1001
                && entry.Source.Equals("Windows Error Reporting", StringComparison.OrdinalIgnoreCase))
            {
                // See if we've already put this into our Dictionary.
                if (reports.TryGetValue(entry.ReplacementStrings[19], out WatsonReport? report))
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
                }
            }
        }

        return reports.Values.ToList();
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }
}
