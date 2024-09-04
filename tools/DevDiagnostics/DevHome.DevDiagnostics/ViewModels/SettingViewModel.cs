// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.DevDiagnostics.Models;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class SettingViewModel : ObservableObject
{
    private readonly Setting _setting;

    private readonly SettingsPageViewModel _settingsPageViewModel;

    public SettingViewModel(Setting setting, SettingsPageViewModel settingsPageViewModel)
    {
        _setting = setting;
        _settingsPageViewModel = settingsPageViewModel;
    }

    public string Path => _setting.Path;

    public string Header => _setting.Header;

    public string Description => _setting.Description;

    public string Glyph => _setting.Glyph;

    [RelayCommand]
    private void NavigateSettings()
    {
        _settingsPageViewModel.Navigate(_setting.Path);
    }
}
