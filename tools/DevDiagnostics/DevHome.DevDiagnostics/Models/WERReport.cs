// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.DevDiagnostics.Helpers;
using Serilog;

namespace DevHome.DevDiagnostics.Models;

public partial class WERReport : ObservableObject
{
    private static readonly string _noAnalysisAvailable = CommonHelper.GetLocalizedString("DumpAnalysisUnavailable");

    public WERBasicReport BasicReport { get; }

    private WERAnalysisReport? _analysisReport;

    [ObservableProperty]
    private string _rawAnalysis = _noAnalysisAvailable;

    [ObservableProperty]
    private string _analysis = _noAnalysisAvailable;

    [ObservableProperty]
    private string _failureBucket = string.Empty;

    public WERReport(WERBasicReport report)
    {
        this.BasicReport = report;
        FailureBucket = report.FailureBucket;
    }

    public void SetAnalysis(string analysis)
    {
        RawAnalysis = analysis;
        _analysisReport = new WERAnalysisReport(analysis);
        if (_analysisReport is not null && !string.IsNullOrEmpty(_analysisReport.FailureBucket))
        {
            FailureBucket = _analysisReport.FailureBucket;
            Analysis = _analysisReport.Analysis ?? string.Empty;
        }
    }
}
