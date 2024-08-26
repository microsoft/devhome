// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using DevHome.DevInsights.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace DevHome.DevInsights.Models;

public class WinLogsEntry
{
    public WinLogsEntry(DateTime? time, WinLogCategory category, string message, string toolName)
    {
        TimeGenerated = time ?? DateTime.Now;
        Category = category;
        Message = message;
        Tool = toolName;
        SelectedText = message;
    }

    public WinLogCategory Category { get; }

    public DateTime TimeGenerated { get; }

    public string TimeGeneratedString => TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string Tool { get; }

    public string Message { get; }

    public string SelectedText { get; set; }
}

public enum WinLogCategory
{
    Information = 0,
    Error,
    Warning,
    Debug,
}

public enum WinLogsTool
{
    Unknown = 0,
    ETWLogs,
    DebugOutput,
    EventViewer,
    WER,
}
