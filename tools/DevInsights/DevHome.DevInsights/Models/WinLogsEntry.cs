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
    private readonly WinLogCategory _category;
    private readonly string _errorText = CommonHelper.GetLocalizedString("WinLogCategoryError");
    private readonly string _warningText = CommonHelper.GetLocalizedString("WinLogCategoryWarning");
    private readonly string _informationText = CommonHelper.GetLocalizedString("WinLogCategoryInformation");
    private readonly string _debugText = CommonHelper.GetLocalizedString("WinLogCategoryDebug");

    public WinLogsEntry(DateTime? time, WinLogCategory category, string message, string toolName)
    {
        TimeGenerated = time ?? DateTime.Now;
        _category = category;
        Message = message;
        Tool = toolName;
        SelectedText = message;
    }

    public DateTime TimeGenerated { get; }

    public string TimeGeneratedString => TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string Tool { get; }

    public string Category => _category switch
    {
        WinLogCategory.Error => _errorText,
        WinLogCategory.Warning => _warningText,
        WinLogCategory.Information => _informationText,
        WinLogCategory.Debug => _debugText,
        _ => string.Empty,
    };

    public string Message { get; }

    public string SelectedText { get; set; }

    public SolidColorBrush RowColor
    {
        get
        {
            switch (_category)
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
