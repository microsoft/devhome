// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Serilog;

namespace SampleExtension.Helpers;

public static class FileHelper
{
    public static void OpenLogsLocation()
    {
        var log = Log.ForContext("SourceContext", "OpenLogs");
        try
        {
            var logLocation = Environment.GetEnvironmentVariable("DEVHOME_LOGS_ROOT");
            log.Information($"Opening logs at: {logLocation}");
            Process.Start("explorer.exe", $"{logLocation}");
        }
        catch (Exception e)
        {
            log.Error(e, $"Error opening log location");
        }
    }
}
