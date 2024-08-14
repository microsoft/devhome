// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Serilog;

namespace SampleExtension.Helpers;

public static class FileHelper
{
#pragma warning disable CS8603 // Possible null reference return.
    public static T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        return default;
    }
#pragma warning restore CS8603 // Possible null reference return.

    public static void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonSerializer.Serialize(content);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public static void Delete(string folderPath, string fileName)
    {
        if (!string.IsNullOrEmpty(fileName) && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }

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
