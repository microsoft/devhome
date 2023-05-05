// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace CoreWidgetProvider.Helpers;
internal class IconLoader
{
    private static readonly Dictionary<string, string> Base64ImageRegistry = new ();

    public static string GetIconAsBase64(string filename)
    {
        if (!Base64ImageRegistry.ContainsKey(filename))
        {
            Base64ImageRegistry.Add(filename, ConvertIconToDataString(filename));
        }

        return Base64ImageRegistry[filename];
    }

    private static string ConvertIconToDataString(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Widgets/Assets/", fileName);
        var imageData = Convert.ToBase64String(File.ReadAllBytes(path.ToString()));
        return imageData;
    }
}
