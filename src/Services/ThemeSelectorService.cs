// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Services;

public class ThemeSelectorService : IThemeSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedTheme";

    public event EventHandler<ElementTheme> ThemeChanged = (_, _) => { };

    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public ThemeSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        Theme = await LoadThemeFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        SetRequestedTheme();
        await SaveThemeInSettingsAsync(Theme);
    }

    public void SetRequestedTheme() => ThemeChanged(null, Theme);

    public bool IsDarkTheme()
    {
        // If theme is Default, use the Application.RequestedTheme value
        // https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.elementtheme?view=windows-app-sdk-1.2#fields
        return Theme == ElementTheme.Dark ||
            (Theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
    }

    public ElementTheme GetActualTheme()
    {
        return IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
    }

    private async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        var themeName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, theme.ToString());
    }
}
