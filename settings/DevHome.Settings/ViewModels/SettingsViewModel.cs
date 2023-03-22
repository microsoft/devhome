// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Resources;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using DevHome.Settings.Views;
using Microsoft.UI.Xaml;
using Windows.Devices.Display.Core;
using Windows.Storage;
using Windows.System;

namespace DevHome.Settings.ViewModels;

public partial class SettingViewModel : ObservableRecipient
{
    private readonly Setting _setting;

    private readonly SettingsViewModel _settingsViewModel;

    public SettingViewModel(Setting setting, SettingsViewModel settingsViewModel)
    {
        _setting = setting;
        _settingsViewModel = settingsViewModel;
    }

    public string? LinkName => _setting.LinkName;

    public string? Header => _setting.Header;

    public string? Description => _setting.Description;

    [RelayCommand]
    private void NavigateSettings()
    {
        _settingsViewModel.Navigate(_setting.LinkName);
    }
}

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly ObservableCollection<SettingViewModel> _l1SettingsList = new ();

    [ObservableProperty]
    private ObservableCollection<SettingViewModel> _settingsList;

    public SettingsViewModel()
    {
        _settingsList = _l1SettingsList;

        var stringResource = new StringResource("DevHome.Settings/Resources");

        Setting[] settings = new[]
        {
            new Setting("Preferences", stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description")),
            new Setting("Accounts", stringResource.GetLocalized("Settings_Accounts_Header"), stringResource.GetLocalized("Settings_Accounts_Description")),
            new Setting("Notifications", stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description")),
            new Setting("Plugins", stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description")),
            new Setting("About", stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description")),
        };

        foreach (var setting in settings)
        {
            SettingsList.Add(new SettingViewModel(setting, this));
        }
    }

    public void Navigate(string? linkName)
    {
        var stringResource = new StringResource("DevHome.Settings/Resources");

        Setting[] settings = new[]
        {
            new Setting("L2Test", stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description")),
        };

        SettingsList.Clear();

        foreach (var setting in settings)
        {
            SettingsList.Add(new SettingViewModel(setting, this));
        }
    }
}
