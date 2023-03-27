// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    public string Path => _setting.Path;

    public string Header => _setting.Header;

    public string Description => _setting.Description;

    public bool HasToggleSwitch => _setting.HasToggleSwitch;

    [RelayCommand]
    private void NavigateSettings()
    {
        _settingsViewModel.Navigate(_setting.Path);
    }
}

public partial class SettingsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private ObservableCollection<SettingViewModel> _settingsList = new ();

    public SettingsViewModel()
    {
        var stringResource = new StringResource("DevHome.Settings/Resources");

#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
        Setting[] settings = new[]
        {
            new Setting("Preferences", string.Empty, stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description"), false),
            new Setting("Accounts", string.Empty, stringResource.GetLocalized("Settings_Accounts_Header"), stringResource.GetLocalized("Settings_Accounts_Description"), false),
            // new Setting("Notifications", string.Empty, stringResource.GetLocalized("Settings_Notifications_Header"), stringResource.GetLocalized("Settings_Notifications_Description"), false),
            new Setting("Plugins", string.Empty, stringResource.GetLocalized("Settings_Plugins_Header"), stringResource.GetLocalized("Settings_Plugins_Description"), false),
            new Setting("About", string.Empty, stringResource.GetLocalized("Settings_About_Header"), stringResource.GetLocalized("Settings_About_Description"), false),
        };
#pragma warning restore SA1515 // Single-line comment should be preceded by blank line

        foreach (var setting in settings)
        {
            SettingsList.Add(new SettingViewModel(setting, this));
        }
    }

    public void Navigate(string path)
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        var segments = path.Split("/");
        switch (segments[0])
        {
            case "Preferences":
                navigationService.NavigateTo(typeof(PreferencesViewModel).FullName!);
                return;
            case "Accounts":
                navigationService.NavigateTo(typeof(AccountsViewModel).FullName!);
                return;
            case "Notifications":
                navigationService.NavigateTo(typeof(NotificationsViewModel).FullName!);
                return;
            case "Plugins":
                navigationService.NavigateTo(typeof(PluginsViewModel).FullName!);
                return;
            case "About":
                navigationService.NavigateTo(typeof(AboutViewModel).FullName!);
                return;
            default:
                return;
        }
    }
}
