// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Helpers;

internal sealed class EventViewerHelper : IDisposable
{
    private readonly Process targetProcess;
    private readonly EventLogWatcher? eventLogWatcher;

    public EventViewerHelper(Process targetProcess)
    {
        this.targetProcess = targetProcess;
        this.targetProcess.Exited += TargetProcess_Exited;

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
            var winlogsViewModel = Application.Current.GetService<WinLogsPageViewModel>();
            winlogsViewModel.AddNewEntry(DateTime.Now, WinLogCategory.Error, message, WinLogsHelper.EventViewerName);
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
            var winlogsViewModel = Application.Current.GetService<WinLogsPageViewModel>();
            winlogsViewModel.AddNewEntry(eventArg.EventRecord.TimeCreated, category, message, WinLogsHelper.EventViewerName);
        }
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
    }
}
