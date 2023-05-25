// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DevHome.ViewModels;

public partial class FeedbackViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    public FeedbackViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(ElementTheme theme)
    {
        if (ElementTheme != theme)
        {
            ElementTheme = theme;

            await _themeSelectorService.SetThemeAsync(theme);
        }
    }

    private static string GetVersionDescription()
    {
        IAppInfoService appInfoService = Application.Current.GetService<IAppInfoService>();
        var localizedAppName = appInfoService.GetAppNameLocalized();
        var version = appInfoService.GetAppVersion();

        return $"{localizedAppName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
