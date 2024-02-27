// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Settings.Models;

namespace DevHome.Settings.ViewModels;

public partial class SettingViewModel : ObservableObject
{
    private readonly Setting _setting;

    private readonly SettingsViewModel _settingsViewModel;

    public SettingViewModel(Setting setting, SettingsViewModel settingsViewModel)
    {
        _setting = setting;
        _settingsViewModel = settingsViewModel;
    }

    public string Path => _setting.Path;

    public string Header => _setting.Header;

    public string Description => _setting.Description;

    public string Glyph => _setting.Glyph;

    public bool HasToggleSwitch => _setting.HasToggleSwitch;

    [RelayCommand]
    private void NavigateSettings()
    {
        _settingsViewModel.Navigate(_setting.Path);
    }
}
