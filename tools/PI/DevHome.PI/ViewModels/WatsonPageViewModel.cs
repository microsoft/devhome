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
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly ObservableCollection<WatsonReport> _reports;
    private Process? _targetProcess;
    private WatsonHelper? _watsonHelper;
    private Thread? _watsonThread;

    [ObservableProperty]
    private ObservableCollection<WatsonReport> _reportEntries;

    [ObservableProperty]
    private string _watsonInfoText;

    public WatsonPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _watsonInfoText = string.Empty;
        _reports = new();
        _reportEntries = new();
        _reports.CollectionChanged += WatsonOutput_CollectionChanged;

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    public void UpdateTargetProcess(Process process)
    {
        if (_targetProcess != process)
        {
            _targetProcess = process;

            StopWatsonHelper();
            _watsonThread = new Thread(StartWatsonHelper);
            _watsonThread.Name = "Watson Page Thread";
            _watsonThread.Start();
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
        if (_targetProcess is not null)
        {
            _watsonHelper = new WatsonHelper(_targetProcess, _reports, null);
            _watsonHelper.Start();

            // Get all existing reports
            List<WatsonReport> existingReports = _watsonHelper.GetWatsonReports();
            foreach (var report in existingReports)
            {
                _reports.Add(report);
            }
        }
    }

    private void StopWatsonHelper(bool shouldCleanLogs = true)
    {
        _watsonHelper?.Stop();

        if (Thread.CurrentThread != _watsonThread)
        {
            _watsonThread?.Join();
        }

        if (shouldCleanLogs)
        {
            ClearWatsonLogs();
        }
    }

    public void Dispose()
    {
        _watsonHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void WatsonOutput_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            _dispatcher.TryEnqueue(() =>
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
        _reports?.Clear();

        _dispatcher.TryEnqueue(() =>
        {
            ReportEntries.Clear();
        });
    }
}
