// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace CoreWidgetProvider.Helpers;

public class IconLoader
{
    private static readonly Dictionary<string, string> Base64ImageRegistry = new();

    public static string GetIconAsBase64(string filename)
    {
        var log = Log.ForContext("SourceContext", nameof(IconLoader));
        log.Debug(nameof(IconLoader), $"Asking for icon: {filename}");
        if (!Base64ImageRegistry.TryGetValue(filename, out var value))
        {
            value = ConvertIconToDataString(filename);
            Base64ImageRegistry.Add(filename, value);
            log.Debug(nameof(IconLoader), $"The icon {filename} was converted and is now stored.");
        }

        return value;
    }

    private static string ConvertIconToDataString(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, @"Widgets/Assets/", fileName);
        var imageData = Convert.ToBase64String(File.ReadAllBytes(path.ToString()));
        return imageData;
    }
}
