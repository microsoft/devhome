// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.Services;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Helpers;

internal sealed class EventViewerHelper : IDisposable
{
    private readonly Process _targetProcess;
    private readonly EventLogWatcher? _eventLogWatcher;
    private readonly WinLogsService _winLogsService;

    public EventViewerHelper(Process targetProcess)
    {
        _targetProcess = targetProcess;
        _targetProcess.Exited += TargetProcess_Exited;
        _winLogsService = Application.Current.GetService<WinLogsService>();

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
            _winLogsService.AddWinLogsEntry(entry);
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
            _winLogsService.AddWinLogsEntry(entry);
        }
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }
}
