// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public partial class PreferencesViewModel : BreadcrumbViewModel
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    public override ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public PreferencesViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_Preferences_Header"), typeof(PreferencesViewModel).FullName!),
        };
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(ElementTheme elementTheme)
    {
        ElementTheme = elementTheme;
        await _themeSelectorService.SetThemeAsync(elementTheme);
    }
}
