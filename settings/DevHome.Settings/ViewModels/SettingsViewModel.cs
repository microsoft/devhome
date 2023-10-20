// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;

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

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SettingViewModel> _settingsList = new ();

    public SettingsViewModel()
    {
        var stringResource = new StringResource("DevHome.Settings/Resources");

        var settings = new[]
        {
            new Setting("Preferences", string.Empty, stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description"), "\ue713", false, false),
            new Setting("Accounts", string.Empty, stringResource.GetLocalized("Settings_Accounts_Header"), stringResource.GetLocalized("Settings_Accounts_Description"), "\ue77b", false, false),
            new Setting("Extensions", string.Empty, stringResource.GetLocalized("Settings_Extensions_Header"), stringResource.GetLocalized("Settings_Extensions_Description"), "\ued35", false, false),
            new Setting("About", string.Empty, stringResource.GetLocalized("Settings_About_Header"), stringResource.GetLocalized("Settings_About_Description"), "\ue946", false, false),
            new Setting("Feedback", string.Empty, stringResource.GetLocalized("Settings_Feedback_Header"), stringResource.GetLocalized("Settings_Feedback_Description"), "\ued15", false, false),
            new Setting("ExperimentalFeatures", string.Empty, stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), stringResource.GetLocalized("Settings_ExperimentalFeatures_Description"), "\ue74c", false, false),
        };

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
            case "Extensions":
                navigationService.NavigateTo(typeof(ExtensionsViewModel).FullName!);
                return;
            case "About":
                navigationService.NavigateTo(typeof(AboutViewModel).FullName!);
                return;
            case "Feedback":
                navigationService.NavigateTo(typeof(FeedbackViewModel).FullName!);
                return;
            case "ExperimentalFeatures":
                navigationService.NavigateTo(typeof(ExperimentalFeaturesViewModel).FullName!);
                return;
            default:
                return;
        }
    }
}
