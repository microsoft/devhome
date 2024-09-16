// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.Models;

public class ThemeName
{
    public string Name { get; set; } = string.Empty;

    public ElementTheme Theme { get; set; }

    public ThemeName(string name, ElementTheme theme) => (Name, Theme) = (name, theme);

    public static List<ThemeName> Themes { get; private set; } =
    [
        new ThemeName("Light", ElementTheme.Light),
        new ThemeName("Dark", ElementTheme.Dark),
        new ThemeName("Default", ElementTheme.Default)
    ];
}
