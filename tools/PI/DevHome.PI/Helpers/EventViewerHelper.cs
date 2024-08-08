// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using DevHome.PI.Models;
using DevHome.PI.Services;

namespace DevHome.PI.Helpers;

internal sealed class EventViewerHelper : IDisposable
{
    private readonly Process _targetProcess;
    private readonly ObservableCollection<WinLogsEntry> _output;
    private readonly EventLogWatcher? _eventLogWatcher;

    public EventViewerHelper(Process targetProcess, ObservableCollection<WinLogsEntry> output)
    {
        _targetProcess = targetProcess;
        _targetProcess.Exited += TargetProcess_Exited;
        _output = output;

        try
        {
            // Subscribe for Application events matching the processName.
            var filterQuery = "*[System[Provider[@Name=\"" + targetProcess.ProcessName + "\"]]]";
            EventLogQuery subscriptionQuery = new("Application", PathType.LogName, filterQuery);
            _eventLogWatcher = new EventLogWatcher(subscriptionQuery);
            _eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventLogEventRead);
        }
        catch (EventLogReadingException)
        {
            var message = CommonHelper.GetLocalizedString("UnableToStartEventViewerErrorMessage");
            WinLogsEntry entry = new(DateTime.Now, WinLogCategory.Error, message, WinLogsService.EventViewerName);
            output.Add(entry);
        }
    }

    public void Start()
    {
        if (_eventLogWatcher is not null)
        {
            _eventLogWatcher.Enabled = true;
        }
    }

    public void Stop()
    {
        if (_eventLogWatcher is not null)
        {
            _eventLogWatcher.Enabled = false;
        }
    }

    public void Dispose()
    {
        if (_eventLogWatcher is not null)
        {
            _eventLogWatcher.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public void EventLogEventRead(object? obj, EventRecordWrittenEventArgs eventArg)
    {
        if (eventArg.EventRecord != null)
        {
            WinLogCategory category = WinLogsService.ConvertStandardEventLevelToWinLogCategory(eventArg.EventRecord.Level);
            var message = eventArg.EventRecord.FormatDescription();
            WinLogsEntry entry = new(eventArg.EventRecord.TimeCreated, category, message, WinLogsService.EventViewerName);
            _output.Add(entry);
        }
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }
}
