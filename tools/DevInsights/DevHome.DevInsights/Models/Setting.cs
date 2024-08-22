// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.DevInsights.Models;

public class Setting
{
    public string Path { get; }

    public string Header { get; }

    public string Description { get; }

    public string Glyph { get; }

    public Setting(string path, string header, string description, string glyph)
    {
        Path = path;
        Header = header;
        Description = description;
        Glyph = glyph;
    }
}
