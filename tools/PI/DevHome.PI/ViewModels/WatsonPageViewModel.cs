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
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class WatsonPageViewModel : ObservableObject, IDisposable
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;
    private Process? targetProcess;
    private WatsonHelper? watsonHelper;
    private Thread? watsonThread;

    [ObservableProperty]
    private ObservableCollection<WatsonReport> reportEntries;

    public WatsonPageViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        reportEntries = new();

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

    public void Dispose()
    {
        watsonHelper?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void AddNewEntry(DateTime timeGenerated, string moduleName, string executable, string eventGuid)
    {
        var newEntry = new WatsonReport(timeGenerated, moduleName, executable, eventGuid);
        dispatcher.TryEnqueue(() =>
        {
            ReportEntries.Add(newEntry);
        });
    }

    public void UpdateEntry(string eventGuid, string watsonLog, string directoryPath)
    {
        dispatcher.TryEnqueue(() =>
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
        if (targetProcess is not null)
        {
            watsonHelper = new WatsonHelper(targetProcess);
            watsonHelper.Start();

            // Get all existing reports
            watsonHelper.GetExistingWatsonReports();
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

    private void ClearWatsonLogs()
    {
        dispatcher.TryEnqueue(() =>
        {
            ReportEntries.Clear();
        });
    }
}
