// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace DevHome.Contracts.Services;

public interface IThemeSelectorService
{
    /// <summary>
    /// Occurs when the theme has changed, either due to user selection or the system theme changing.
    /// </summary>
    public event EventHandler<ElementTheme> ThemeChanged;

    ElementTheme Theme { get; }

    Task InitializeAsync();

    Task SetThemeAsync(ElementTheme theme);

    void SetRequestedTheme();

    /// <summary>
    /// Checks if the <see cref="Theme"/> value resolves to dark
    /// </summary>
    /// <returns>True if the current theme is dark</returns>
    bool IsDarkTheme();

    ElementTheme GetActualTheme();
}
