// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.PI.Models;

public partial class WERAnalysisReport : ObservableObject
{
    private readonly ExternalToolsHelper _externalTools;
    private readonly Dictionary<Tool, WERAnalysis> _toolAnalyses = new();

    public WERReport Report { get; }

    public ReadOnlyDictionary<Tool, WERAnalysis> ToolAnalyses { get; private set; }

    public WERAnalysisReport(WERReport report)
    {
        Report = report;
        ToolAnalyses = new(_toolAnalyses);
        _externalTools = Application.Current.GetService<ExternalToolsHelper>();
    }

    public void RunToolAnalysis(Tool tool)
    {
        Debug.Assert(tool.Type.HasFlag(ToolType.DumpAnalyzer), "We should only be running dump analyzers on dumps");

        WERAnalysis analysis = new(tool, Report.CrashDumpPath);
        analysis.Run();
        if (analysis.Analysis is not null)
        {
            _toolAnalyses.Add(tool, analysis);
        }
    }

    public void RemoveToolAnalysis(Tool tool)
    {
        Debug.Assert(tool.Type.HasFlag(ToolType.DumpAnalyzer), "We should only be running dump analyzers on dumps");

        WERAnalysis? analysis;

        if (_toolAnalyses.TryGetValue(tool, out analysis))
        {
            analysis.RemoveCachedResults();
            _toolAnalyses.Remove(tool);
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
    }

    public void Run()
    {
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
            options.RedirectStandardOut = true;

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
        return _crashDumpPath + "." + AnalysisTool.Name + ".analysisresults";
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
