// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Helpers;

internal sealed class WatsonHelper : IDisposable
{
    private const string WatsonQueryPart1 = "(*[System[Provider[@Name=\"Application Error\"]]] and *[System[EventID=1000]])";
    private const string WatsonQueryPart2 = "(*[System[Provider[@Name=\"Windows Error Reporting\"]]] and *[System[EventID=1001]])";

    private readonly Process targetProcess;
    private readonly EventLogWatcher? eventLogWatcher;

    public WatsonHelper(Process targetProcess)
    {
        this.targetProcess = targetProcess;
        this.targetProcess.Exited += TargetProcess_Exited;

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
            var winlogsViewModel = Application.Current.GetService<WinLogsPageViewModel>();
            winlogsViewModel.AddNewEntry(DateTime.Now, WinLogCategory.Error, message, WinLogsHelper.WatsonName);
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

                    var watsonViewModel = Application.Current.GetService<WatsonPageViewModel>();
                    watsonViewModel.AddNewEntry(timeGenerated, moduleName, executable, eventGuid);

                    var winlogsViewModel = Application.Current.GetService<WinLogsPageViewModel>();
                    winlogsViewModel.AddNewEntry(timeGenerated, WinLogCategory.Error, eventRecord.FormatDescription(), WinLogsHelper.WatsonName);
                }
            }
            else if (eventRecord.Id == 1001 && eventRecord.ProviderName.Equals("Windows Error Reporting", StringComparison.OrdinalIgnoreCase))
            {
                var eventGuid = eventRecord.Properties[19].Value.ToString() ?? string.Empty;
                var watsonLog = eventRecord.FormatDescription();
                var directoryPath = eventRecord.Properties[16].Value.ToString() ?? string.Empty;

                var watsonViewModel = Application.Current.GetService<WatsonPageViewModel>();
                watsonViewModel.UpdateEntry(eventGuid, watsonLog, directoryPath);
            }
        }
    }

    public void LoadExistingWatsonReports()
    {
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

                var watsonViewModel = Application.Current.GetService<WatsonPageViewModel>();
                watsonViewModel.AddNewEntry(timeGenerated, moduleName, executable, eventGuid);
            }
            else if (entry.InstanceId == 1001
                && entry.Source.Equals("Windows Error Reporting", StringComparison.OrdinalIgnoreCase))
            {
                var eventGuid = entry.ReplacementStrings[19];
                var watsonLog = entry.Message;
                var directoryPath = entry.ReplacementStrings[16];

                var watsonViewModel = Application.Current.GetService<WatsonPageViewModel>();
                watsonViewModel.UpdateEntry(eventGuid, watsonLog, directoryPath);
            }
        }
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }
}
