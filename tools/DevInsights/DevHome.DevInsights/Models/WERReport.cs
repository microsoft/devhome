// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.DevInsights.Models;

public partial class WERReport : ObservableObject
{
    [ObservableProperty]
    private DateTime _timeStamp;

    public string TimeGenerated => TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _module = string.Empty;

    [ObservableProperty]
    private string _executable = string.Empty;

    [ObservableProperty]
    private string _eventGuid = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _pid = 0;

    [ObservableProperty]
    private string _crashDumpPath = string.Empty;

    [ObservableProperty]
    private string _failureBucket = string.Empty;

    public WERReport()
    {
    }
}
