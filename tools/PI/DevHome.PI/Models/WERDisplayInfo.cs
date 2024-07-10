// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.PI.Models;

public partial class WERDisplayInfo : ObservableObject
{
    public WERReport Report { get; }

    [ObservableProperty]
    private string _failureBucket;

    [ObservableProperty]
    private string _analyzeResults;

    public WERDisplayInfo(WERReport report)
    {
        Report = report;
        AnalyzeResults = InitializeAnalyzeResults();
        FailureBucket = UpdateFailureBucket();
        Report.PropertyChanged += Report_PropertyChanged;
    }

    private void Report_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WERReport.CrashDumpPath))
        {
            AnalyzeResults = InitializeAnalyzeResults();
            FailureBucket = UpdateFailureBucket();
        }
        else if (e.PropertyName == nameof(WERReport.FailureBucket))
        {
            FailureBucket = UpdateFailureBucket();
        }
    }

    private string UpdateFailureBucket()
    {
        if (AnalyzeResults == string.Empty)
        {
            return Report.FailureBucket;
        }

        string[] lines = AnalyzeResults.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.Contains("FAILURE_BUCKET_ID:"))
            {
                return line.Substring(line.IndexOf(':') + 1).Trim();
            }
        }

        // If we weren't able to get a failure bucket from !analyze results, return the one from the WER data
        return Report.FailureBucket;
    }

    private string InitializeAnalyzeResults()
    {
        if (Report.CrashDumpPath is null || Report.CrashDumpPath == string.Empty)
        {
            return string.Empty;
        }

        // Where the analysis file should be....
        var analysisFile = Report.CrashDumpPath + ".analyze";

        if (!File.Exists(analysisFile))
        {
            return "Cab has not been analyzed yet";
        }

        try
        {
            return File.ReadAllText(analysisFile);
        }
        catch
        {
        }

        return "Unable to access data";
    }
}
