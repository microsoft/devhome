// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Helpers;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.Models;

// This class monitors for WER reports and runs analysis on them
public class WERAnalyzer : IDisposable
{
    private readonly WERHelper _werHelper;
    private readonly ExternalToolsHelper _externalToolsHelper;

    private struct AnalysisRequest
    {
        public WERAnalysisReport Report;
        public Tool Tool;
    }

    private readonly BlockingCollection<AnalysisRequest> _analysisRequests = new();

    private readonly ObservableCollection<WERAnalysisReport> _werAnalysisReports = [];

    public ReadOnlyObservableCollection<WERAnalysisReport> WERAnalysisReports { get; private set; }

    private readonly ObservableCollection<Tool> _registeredAnalysisTools = [];

    public ReadOnlyObservableCollection<Tool> RegisteredAnalysisTools { get; private set; }

    public WERAnalyzer()
    {
        WERAnalysisReports = new(_werAnalysisReports);
        RegisteredAnalysisTools = new(_registeredAnalysisTools);

        _externalToolsHelper = Application.Current.GetService<ExternalToolsHelper>();

        _werHelper = Application.Current.GetService<WERHelper>();
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WER_CollectionChanged;

        // Collect all of the tools used for dump analysis
        foreach (Tool tool in _externalToolsHelper.AllExternalTools)
        {
            if (tool.Type.HasFlag(ToolType.DumpAnalyzer))
            {
                _registeredAnalysisTools.Add(tool);
            }
        }

        ((INotifyCollectionChanged)_externalToolsHelper.AllExternalTools).CollectionChanged += AllExternalTools_CollectionChanged;
        PopulateCurrentLogs();

        // Create a dedicated thread to serially perform all of our crash dump analysis
        var crashDumpAnalyzerThread = new Thread(() =>
        {
            while (!_analysisRequests.IsCompleted)
            {
                if (_analysisRequests.TryTake(out AnalysisRequest request, Timeout.Infinite))
                {
                    request.Report.RunToolAnalysis(request.Tool);
                }
            }
        });
        crashDumpAnalyzerThread.Name = "CrashDumpAnalyzerThread";
        crashDumpAnalyzerThread.Start();
    }

    private void AllExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // If we have a new tool, we'll want to perform analysis on the crash dump
        // with it.
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (Tool tool in e.NewItems)
            {
                if (tool.Type.HasFlag(ToolType.DumpAnalyzer))
                {
                    _registeredAnalysisTools.Add(tool);
                }
            }

            foreach (var report in _werAnalysisReports)
            {
                RunToolAnalysis(report, e.NewItems.Cast<Tool>().ToList());
            }
        }

        // Or if we removed a tool, remove the analysis that it did.
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (Tool tool in e.OldItems)
            {
                if (tool.Type.HasFlag(ToolType.DumpAnalyzer))
                {
                    _registeredAnalysisTools.Remove(tool);
                }

                foreach (var report in _werAnalysisReports)
                {
                    report.RemoveToolAnalysis(tool);
                }
            }
        }
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            ProcessDumpList(e.NewItems.Cast<WERReport>().ToList());
        }
    }

    private void PopulateCurrentLogs()
    {
        ProcessDumpList(_werHelper.WERReports.ToList<WERReport>());
    }

    private void ProcessDumpList(List<WERReport> reports)
    {
        List<WERAnalysisReport> reportsToAnalyze = new();

        // First publish all of these reports to our listeners. Then we'll go back and perform
        // analysis on them.
        foreach (var report in reports)
        {
            var reportAnalysis = new WERAnalysisReport(report);

            _werAnalysisReports.Add(reportAnalysis);
            reportsToAnalyze.Add(reportAnalysis);

            // When the crash dump path changes, we'll want to perform analysis on it.
            report.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(WERReport.CrashDumpPath))
                {
                    RunToolAnalysis(reportAnalysis, RegisteredAnalysisTools.ToList<Tool>());
                }
            };
        }

        List<Tool> tools = RegisteredAnalysisTools.ToList<Tool>();
        foreach (var reportAnalysis in reportsToAnalyze)
        {
            RunToolAnalysis(reportAnalysis, tools);
        }
    }

    private void RunToolAnalysis(WERAnalysisReport report, List<Tool> tools)
    {
        if (string.IsNullOrEmpty(report.Report.CrashDumpPath))
        {
            // We need a crash dump to perform an analysis
            return;
        }

        foreach (Tool tool in tools)
        {
            AnalysisRequest request = new()
            {
                Report = report,
                Tool = tool,
            };

            // Queue the request that will be processed on a separate thread
            _analysisRequests.Add(request);
        }
    }

    public void Dispose()
    {
        _analysisRequests.CompleteAdding();
        _analysisRequests.Dispose();
        GC.SuppressFinalize(this);
    }
}
