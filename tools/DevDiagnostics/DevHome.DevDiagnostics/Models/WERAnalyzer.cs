// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.DevDiagnostics.Models;

// This class monitors for WER reports and runs analysis on them
public class WERAnalyzer : IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WERAnalyzer));
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly WERHelper _werHelper;

    private readonly BlockingCollection<WERReport> _analysisRequests = new();

    private readonly ObservableCollection<WERReport> _werReports = [];

    public ReadOnlyObservableCollection<WERReport> WERReports { get; private set; }

    public WERAnalyzer()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Debug.Assert(_dispatcher is not null, "Need to create this object on the UI thread");

        WERReports = new(_werReports);

        _werHelper = Application.Current.GetService<WERHelper>();
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WER_CollectionChanged;

        PopulateCurrentLogs();

        // Create a dedicated thread to serially perform all of our crash dump analysis
        var crashDumpAnalyzerThread = new Thread(() =>
        {
            while (!_analysisRequests.IsCompleted)
            {
                if (_analysisRequests.TryTake(out var report, Timeout.Infinite))
                {
                    PerformAnalysis(report);
                }
            }
        });
        crashDumpAnalyzerThread.Name = "CrashDumpAnalyzerThread";
        crashDumpAnalyzerThread.Start();
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            ProcessDumpList(e.NewItems.Cast<WERBasicReport>().ToList());
        }
    }

    private void PopulateCurrentLogs()
    {
        ProcessDumpList(_werHelper.WERReports.ToList<WERBasicReport>());
    }

    private void ProcessDumpList(List<WERBasicReport> reports)
    {
        List<WERReport> reportsToAnalyze = new();

        // First publish all of these basic reports to our listeners. Then we'll go back and perform
        // analysis on them.
        foreach (var basicReport in reports)
        {
            var reportAnalysis = new WERReport(basicReport);

            _werReports.Add(reportAnalysis);
            reportsToAnalyze.Add(reportAnalysis);

            // When the crash dump path changes, we'll want to perform analysis on it.
            basicReport.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(WERBasicReport.CrashDumpPath))
                {
                    RunToolAnalysis(reportAnalysis);
                }
            };
        }

        foreach (var reportAnalysis in reportsToAnalyze)
        {
            RunToolAnalysis(reportAnalysis);
        }
    }

    private void RunToolAnalysis(WERReport report)
    {
        if (string.IsNullOrEmpty(report.BasicReport.CrashDumpPath))
        {
            // We need a crash dump to perform an analysis
            return;
        }

        // Queue the request that will be processed on a separate thread
        _analysisRequests.Add(report);
    }

    public void Dispose()
    {
        _analysisRequests.CompleteAdding();
        _analysisRequests.Dispose();
        GC.SuppressFinalize(this);
    }

    public void PerformAnalysis(WERReport report)
    {
        // See if we have a cached analysis
        var analysisFilePath = GetCachedResultsFileName(report);

        if (File.Exists(analysisFilePath))
        {
            string analysis = File.ReadAllText(analysisFilePath);

            _dispatcher.TryEnqueue(() =>
            {
                report.SetAnalysis(analysis);
            });
        }
        else
        {
            // Generate the analysis
            try
            {
                FileInfo fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);

                var startInfo = new ProcessStartInfo()
                {
                    FileName = "DumpAnalyzer.exe",
                    Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"", report.BasicReport.CrashDumpPath, analysisFilePath),
                    UseShellExecute = true,
                    WorkingDirectory = fileInfo.DirectoryName,
                };

                var process = Process.Start(startInfo);
                Debug.Assert(process != null, "If process launch fails, Process.Start should throw an exception");

                process.WaitForExit();

                if (File.Exists(analysisFilePath))
                {
                    string analysis = File.ReadAllText(analysisFilePath);

                    _dispatcher.TryEnqueue(() =>
                    {
                        report.SetAnalysis(analysis);
                    });
                }
                else
                {
                    // Our analysis failed to work. Log the error
                    _log.Error("Error Analyzing " + report.BasicReport.CrashDumpPath);

                    if (File.Exists(analysisFilePath + ".err"))
                    {
                        _log.Error(File.ReadAllText(analysisFilePath + ".err"));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
    }

    private string GetCachedResultsFileName(WERReport report)
    {
        return report.BasicReport.CrashDumpPath + ".analysisresults.xml";
    }

    public void RemoveCachedResults(WERReport report)
    {
        var analysisFilePath = GetCachedResultsFileName(report);

        if (File.Exists(analysisFilePath))
        {
            try
            {
                File.Delete(analysisFilePath);
            }
            catch (Exception ex)
            {
                _log.Warning("Failed to delete cache analysis results - " + ex.ToString());
            }
        }
    }
}
