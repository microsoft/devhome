// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace DevHome.Contracts.Services;

public interface IThemeSelectorService
{
    public event EventHandler<ElementTheme> ThemeChanged;

    ElementTheme Theme
    {
        get;
    }

    Task InitializeAsync();

    Task SetThemeAsync(ElementTheme theme);

    void SetRequestedTheme();

    /// <summary>
    /// Checks if the <see cref="Theme"/> value resolves to dark
    /// </summary>
    /// <returns>True if the current theme is dark</returns>
    bool IsDarkTheme();
}
