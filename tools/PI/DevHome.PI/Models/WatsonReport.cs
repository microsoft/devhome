// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace DevHome.PI.Models;

public class WatsonReport
{
    public DateTime DateTimeGenerated { get; }

    public string TimeGenerated => DateTimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string Module { get; }

    public string Executable { get; }

    public string EventGuid { get; }

    public string? WatsonLog { get; set; }

    public string? WatsonReportFile { get; set; }

    public WatsonReport(DateTime timeGenerated, string moduleName, string executable, string eventGuid)
    {
        this.DateTimeGenerated = timeGenerated;
        Module = moduleName;
        Executable = executable;
        EventGuid = eventGuid;
    }
}
