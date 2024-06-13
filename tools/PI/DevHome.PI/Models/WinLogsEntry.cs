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
    private readonly WinLogCategory category;
    private readonly string errorText = CommonHelper.GetLocalizedString("WinLogCategoryError");
    private readonly string warningText = CommonHelper.GetLocalizedString("WinLogCategoryWarning");
    private readonly string informationText = CommonHelper.GetLocalizedString("WinLogCategoryInformation");
    private readonly string debugText = CommonHelper.GetLocalizedString("WinLogCategoryDebug");

    public WinLogsEntry(DateTime? time, WinLogCategory category, string message, string toolName)
    {
        DateTimeGenerated = time ?? DateTime.Now;
        this.category = category;
        this.Message = message;
        this.Tool = toolName;
        this.SelectedText = message;
    }

    public DateTime DateTimeGenerated { get; }

    public string TimeGenerated => DateTimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string Tool { get; }

    public string Category => category switch
    {
        WinLogCategory.Error => errorText,
        WinLogCategory.Warning => warningText,
        WinLogCategory.Information => informationText,
        WinLogCategory.Debug => debugText,
        _ => string.Empty,
    };

    public string Message { get; }

    public string SelectedText { get; set; }

    public SolidColorBrush RowColor
    {
        get
        {
            switch (category)
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
    Watson,
}
