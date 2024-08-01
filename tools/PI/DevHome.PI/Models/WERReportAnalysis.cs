// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.PI.Models;

public partial class WERReportAnalysis : ObservableObject
{
    private readonly ExternalToolsHelper _externalTools;

    private bool _performDelayedAnalysis;

    public WERReport Report { get; }

    public ObservableCollection<WERAnalysis> Analyses { get; } = new();

    public WERReportAnalysis(WERReport report)
    {
        Report = report;
        Report.PropertyChanged += Report_PropertyChanged;
        _externalTools = Application.Current.GetService<ExternalToolsHelper>();
        ((INotifyCollectionChanged)_externalTools.AllExternalTools).CollectionChanged += AllExternalTools_CollectionChanged;
    }

    private void AllExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ThreadPool.QueueUserWorkItem((o) =>
        {
            // If we have a new tool, we'll want to perform analysis on the crash dump
            // with it.
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (Tool tool in e.NewItems)
                {
                    RunToolAnalysis(tool);
                }
            }

            // Or if we removed a tool, remove the analysis that it did.
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
            {
                foreach (Tool tool in e.OldItems)
                {
                    RemoveToolAnalysis(tool);
                }
            }
        });
    }

    private void Report_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WERReport.CrashDumpPath))
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                UpdateAnalysis();
            });
        }
    }

    private void UpdateAnalysis()
    {
        lock (this)
        {
            if (!_performDelayedAnalysis)
            {
                return;
            }
        }

        PerformAnalysis();
    }

    public void PerformAnalysis()
    {
        lock (this)
        {
            // Do we have a crash dump to analyze? If not, we'll perform the
            // analysis once we get it.
            if (string.IsNullOrEmpty(Report.CrashDumpPath))
            {
                _performDelayedAnalysis = true;
                return;
            }
        }

        // Run through and analyze the crash dump with each of our analyizers
        foreach (Tool tool in _externalTools.AllExternalTools)
        {
            RunToolAnalysis(tool);
        }
    }

    private void RunToolAnalysis(Tool tool)
    {
        if (tool.Type.HasFlag(ToolType.DumpAnalyzer))
        {
            WERAnalysis analysis = new(tool, Report.CrashDumpPath);

            if (analysis.Analysis is not null)
            {
                Analyses.Add(analysis);
            }
        }
    }

    private void RemoveToolAnalysis(Tool tool)
    {
        if (tool.Type.HasFlag(ToolType.DumpAnalyzer))
        {

            // See if we have an analysis for this tool
            foreach (WERAnalysis analysis in Analyses)
            {
                if (analysis.AnalysisTool == tool)
                {
                    analysis.RemoveCachedResults();
                    Analyses.Remove(analysis);
                    break;
                }
            }
        }
    }
}

public class WERAnalysis
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WERAnalysis));

    public Tool AnalysisTool { get; private set; }

    public string? Analysis { get; private set; }

    public string? FailureBucket { get; private set; }

    private readonly string _crashDumpPath;

    public WERAnalysis(Tool analysisTool, string crashDumpPath)
    {
        AnalysisTool = analysisTool;
        _crashDumpPath = crashDumpPath;

        // See if we have a cached analysis
        var analysisFilePath = GetCachedResultsFileName();

        if (File.Exists(analysisFilePath))
        {
            Analysis = File.ReadAllText(analysisFilePath);
        }
        else
        {
            // Generate the analysis
            ToolLaunchOptions options = new();
            options.CommandLineParams = _crashDumpPath;

            AnalysisTool.Invoke(options);

            if (options.LaunchedProcess is not null)
            {
                string output = options.LaunchedProcess.StandardOutput.ReadToEnd();
                Analysis = output;

                try
                {
                    // Cache these results
                    File.WriteAllText(analysisFilePath, output);
                }
                catch (Exception ex)
                {
                    // If we can't write the file, we'll just ignore it.
                    // We'll just have to re-analyze the next time.
                    _log.Warning("Failed to cache analysis results - " + ex.ToString());
                }
            }
        }
    }

    private string GetCachedResultsFileName()
    {
        return Path.Combine(_crashDumpPath, AnalysisTool.Name, ".analysisresults");
    }

    public void RemoveCachedResults()
    {
        var analysisFilePath = GetCachedResultsFileName();

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
