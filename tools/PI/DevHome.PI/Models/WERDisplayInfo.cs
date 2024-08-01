// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;

namespace DevHome.PI.Models;

public partial class WERDisplayInfo : ObservableObject
{
    public WERReport Report { get; }

    [ObservableProperty]
    private string _failureBucket;

    public WERDisplayInfo(WERReport report)
    {
        Report = report;
        FailureBucket = UpdateFailureBucket();
        Report.PropertyChanged += Report_PropertyChanged;
    }

    private void Report_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WERReport.CrashDumpPath))
        {
            FailureBucket = UpdateFailureBucket();
        }
        else if (e.PropertyName == nameof(WERReport.FailureBucket))
        {
            FailureBucket = UpdateFailureBucket();
        }
    }

    private string UpdateFailureBucket()
    {
        // When we provide support for pluggable analysis of cabs, we should call the appropriate analysis tool here to create better failure buckets
        return Report.FailureBucket;
    }
}

public class WERAnalysis
{
    public Tool AnalysisTool { get; private set; }

    public string? Analysis { get; private set; }

    public string? FailureBucket { get; private set; }

    private readonly string _crashDumpPath;

    public WERAnalysis(Tool analysisTool, string crashDumpPath)
    {
        AnalysisTool = analysisTool;
        _crashDumpPath = crashDumpPath;

        // See if we have a cached analysis
        var analysisFilePath = Path.Combine(_crashDumpPath, analysisTool.Name, ".analysisresults");

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
                File.WriteAllText(analysisFilePath, output);
                Analysis = File.ReadAllText(analysisFilePath);
            }
        }
    }
}
