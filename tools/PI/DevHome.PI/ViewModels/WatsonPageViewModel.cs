// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class WatsonPageViewModel : ObservableObject, IDisposable
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;
    private readonly ObservableCollection<WatsonReport> reports;
    private Process? targetProcess;
    private WatsonHelper? watsonHelper;
    private Thread? watsonThread;

    [ObservableProperty]
    private ObservableCollection<WatsonReport> reportEntries;

    [ObservableProperty]
    private string watsonInfoText;

    public WatsonPageViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        watsonInfoText = string.Empty;
        reports = new();
        reportEntries = new();
        reports.CollectionChanged += WatsonOutput_CollectionChanged;

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    public void UpdateTargetProcess(Process process)
    {
        if (targetProcess != process)
        {
            targetProcess = process;

            StopWatsonHelper();
            watsonThread = new Thread(StartWatsonHelper);
            watsonThread.Name = "Watson Page Thread";
            watsonThread.Start();
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            if (TargetAppData.Instance.TargetProcess is not null)
            {
                UpdateTargetProcess(TargetAppData.Instance.TargetProcess);
            }
            else
            {
                StopWatsonHelper(false);
            }
        }
        else if (e.PropertyName == nameof(TargetAppData.HasExited))
        {
            StopWatsonHelper(false);
        }
    }

    private void StartWatsonHelper()
    {
        if (targetProcess is not null)
        {
            watsonHelper = new WatsonHelper(targetProcess, reports, null);
            watsonHelper.Start();

            // Get all existing reports
            List<WatsonReport> existingReports = watsonHelper.GetWatsonReports();
            foreach (var report in existingReports)
            {
                reports.Add(report);
            }
        }
    }

    private void StopWatsonHelper(bool shouldCleanLogs = true)
    {
        watsonHelper?.Stop();

        if (Thread.CurrentThread != watsonThread)
        {
            watsonThread?.Join();
        }

        if (shouldCleanLogs)
        {
            ClearWatsonLogs();
        }
    }

    public void Dispose()
    {
        watsonHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void WatsonOutput_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            dispatcher.TryEnqueue(() =>
            {
                foreach (WatsonReport newEntry in e.NewItems)
                {
                    ReportEntries.Add(newEntry);
                }
            });
        }
    }

    private void ClearWatsonLogs()
    {
        reports?.Clear();

        dispatcher.TryEnqueue(() =>
        {
            ReportEntries.Clear();
        });
    }
}
