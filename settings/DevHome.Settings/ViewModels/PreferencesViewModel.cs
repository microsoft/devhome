// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Contracts.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public partial class PreferencesViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    public PreferencesViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;

        _elementTheme = _themeSelectorService.Theme;
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(ElementTheme elementTheme)
    {
        ElementTheme = elementTheme;
        await _themeSelectorService.SetThemeAsync(elementTheme);
    }
}
