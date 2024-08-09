// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using DevHome.PI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace DevHome.PI.Models;

public class WinLogsEntry
{
    public WinLogsEntry(DateTime? time, WinLogCategory category, string message, string toolName)
    {
        this.TimeStamp = time ?? DateTime.Now;
        this.Category = category;
        this.Message = message;
        this.Tool = toolName;
        this.SelectedText = message;
    }

    public WinLogCategory Category { get; }

    public DateTime TimeStamp { get; }

    public string TimeGenerated => TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string Tool { get; }

    public string Message { get; }

    public string SelectedText { get; set; }

    public SolidColorBrush RowColor
    {
        get
        {
            switch (Category)
            {
                case WinLogCategory.Error:
                    return new SolidColorBrush(Colors.Red);
                case WinLogCategory.Warning:
                    return new SolidColorBrush(Colors.Orange);
            }

            return new SolidColorBrush(Colors.Black);
        }
    }
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
