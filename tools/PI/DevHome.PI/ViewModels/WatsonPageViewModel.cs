// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class WatsonPageViewModel : ObservableObject, IDisposable
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private Process? _targetProcess;
    private WatsonHelper? _watsonHelper;
    private Thread? _watsonThread;

    [ObservableProperty]
    private ObservableCollection<WatsonReport> _reportEntries;

    public WatsonPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _reportEntries = new();

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

    public void Dispose()
    {
        _watsonHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void AddNewEntry(DateTime timeGenerated, string moduleName, string executable, string eventGuid)
    {
        var newEntry = new WatsonReport(timeGenerated, moduleName, executable, eventGuid);
        _dispatcher.TryEnqueue(() =>
        {
            ReportEntries.Add(newEntry, entry => entry.DateTimeGenerated);
        });
    }

    public void UpdateEntry(string eventGuid, string watsonLog, string directoryPath)
    {
        _dispatcher.TryEnqueue(() =>
        {
            // See if we've already put this into our Collection.
            for (var i = 0; i < ReportEntries?.Count; i++)
            {
                var existingReport = ReportEntries[i];
                if (existingReport.EventGuid.Equals(eventGuid, StringComparison.OrdinalIgnoreCase))
                {
                    existingReport.WatsonLog = watsonLog;
                    try
                    {
                        // List files available in the archive.
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
        });
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
            _watsonHelper = new WatsonHelper(_targetProcess);
            _watsonHelper.Start();

            // Get all existing reports
            _watsonHelper.LoadExistingWatsonReports();
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

    private void ClearWatsonLogs()
    {
        _dispatcher.TryEnqueue(() =>
        {
            ReportEntries.Clear();
        });
    }
}
