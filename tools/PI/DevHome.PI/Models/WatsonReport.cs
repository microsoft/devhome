// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace DevHome.PI.Models;

public class WatsonReport
{
    public DateTime TimeStamp { get; }

    public string TimeGenerated => TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string FilePath { get; }

    public string Module { get; }

    public string Executable { get; }

    public string EventGuid { get; }

    public string Description { get; set; }

    public string? CrashDumpPath { get; set; }

    public WatsonReport(string filePath, DateTime timeGenerated, string moduleName, string executable, string eventGuid, string description)
    {
        FilePath = filePath;
        TimeStamp = timeGenerated;
        Module = moduleName;
        Executable = executable;
        EventGuid = eventGuid;
        Description = description;
    }
}
