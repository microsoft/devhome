// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using DevHome.DevInsights.Models;

namespace DevHome.DevInsights.Helpers;

internal sealed class EventViewerHelper : IDisposable
{
    private readonly Process targetProcess;
    private readonly ObservableCollection<WinLogsEntry> output;
    private readonly EventLogWatcher? eventLogWatcher;

    public EventViewerHelper(Process targetProcess, ObservableCollection<WinLogsEntry> output)
    {
        this.targetProcess = targetProcess;
        this.targetProcess.Exited += TargetProcess_Exited;
        this.output = output;

        try
        {
            // Subscribe for Application events matching the processName.
            var filterQuery = "*[System[Provider[@Name=\"" + targetProcess.ProcessName + "\"]]]";
            EventLogQuery subscriptionQuery = new("Application", PathType.LogName, filterQuery);
            eventLogWatcher = new EventLogWatcher(subscriptionQuery);
            eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventLogEventRead);
        }
        catch (EventLogReadingException)
        {
            var message = CommonHelper.GetLocalizedString("UnableToStartEventViewerErrorMessage");
            WinLogsEntry entry = new(DateTime.Now, WinLogCategory.Error, message, WinLogsHelper.EventViewerName);
            output.Add(entry);
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
        if (eventArg.EventRecord != null)
        {
            WinLogCategory category = WinLogsHelper.ConvertStandardEventLevelToWinLogCategory(eventArg.EventRecord.Level);
            var message = eventArg.EventRecord.FormatDescription();
            WinLogsEntry entry = new(eventArg.EventRecord.TimeCreated, category, message, WinLogsHelper.EventViewerName);
            output.Add(entry);
        }
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }
}
