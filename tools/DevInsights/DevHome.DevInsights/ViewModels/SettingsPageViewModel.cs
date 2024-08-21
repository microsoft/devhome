// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.DevInsights.Helpers;
using DevHome.DevInsights.Models;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.DevInsights.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(SettingsPageViewModel));

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private ObservableCollection<SettingViewModel> _settingsList = new();

    public SettingsPageViewModel()
    {
        var settings = new[]
        {
            new Setting("Preferences", CommonHelper.GetLocalizedString("SettingsPreferencesHeader"), CommonHelper.GetLocalizedString("SettingsPreferencesSubHeader"), "\uE713"),
            new Setting("AdditionalTools", CommonHelper.GetLocalizedString("SettingsAdditionalToolsHeader"), CommonHelper.GetLocalizedString("SettingsAdditionalToolsSubHeader"), "\uEC7A"),
            new Setting("AdvancedSettings", CommonHelper.GetLocalizedString("SettingsAdvancedSettingsHeader"), CommonHelper.GetLocalizedString("SettingsAdvancedSettingsSubHeader"), "\uE90F"),
            new Setting("About", CommonHelper.GetLocalizedString("SettingsAboutHeader"), CommonHelper.GetLocalizedString("SettingsAboutSubHeader"), "\uE946"),
        };

        foreach (var setting in settings)
        {
            SettingsList.Add(new SettingViewModel(setting, this));
        }

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(CommonHelper.GetLocalizedString("SettingsPageHeader"), typeof(SettingsPageViewModel).FullName!),
        };
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
            case "AdditionalTools":
                navigationService.NavigateTo(typeof(AdditionalToolsViewModel).FullName!);
                return;
            case "AdvancedSettings":
                navigationService.NavigateTo(typeof(AdvancedSettingsViewModel).FullName!);
                return;
            case "About":
                navigationService.NavigateTo(typeof(AboutViewModel).FullName!);
                return;
            default:
                return;
        }
    }
}
