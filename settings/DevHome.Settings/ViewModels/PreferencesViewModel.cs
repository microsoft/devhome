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
    private readonly IExperimentationService _experimentationService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private bool _isExperimentationEnabled;

    public PreferencesViewModel(IThemeSelectorService themeSelectorService, IExperimentationService experimentationService)
    {
        _themeSelectorService = themeSelectorService;
        _experimentationService = experimentationService;

        _elementTheme = _themeSelectorService.Theme;

        _isExperimentationEnabled = _experimentationService.IsExperimentationEnabled;
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(ElementTheme elementTheme)
    {
        ElementTheme = elementTheme;
        await _themeSelectorService.SetThemeAsync(elementTheme);
    }

    [RelayCommand]
    public async Task ExperimentationToggledAsync()
    {
        IsExperimentationEnabled = !IsExperimentationEnabled;

        _experimentationService.IsExperimentationEnabled = IsExperimentationEnabled;

        await Task.CompletedTask;
    }
}
