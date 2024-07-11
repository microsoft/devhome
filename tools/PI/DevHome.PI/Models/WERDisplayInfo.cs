// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

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
