// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private ObservableCollection<SettingViewModel> _settingsList = new();

    public SettingsViewModel()
    {
        var stringResource = new StringResource("DevHome.Settings/Resources");

        var settings = new[]
        {
            new Setting("Preferences", string.Empty, stringResource.GetLocalized("Settings_Preferences_Header"), stringResource.GetLocalized("Settings_Preferences_Description"), "\ue713", false, false),
            new Setting("Accounts", string.Empty, stringResource.GetLocalized("Settings_Accounts_Header"), stringResource.GetLocalized("Settings_Accounts_Description"), "\ue77b", false, false),
            new Setting("ExperimentalFeatures", string.Empty, stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), stringResource.GetLocalized("Settings_ExperimentalFeatures_Description"), "\ue74c", false, false),
            new Setting("Feedback", string.Empty, stringResource.GetLocalized("Settings_Feedback_Header"), stringResource.GetLocalized("Settings_Feedback_Description"), "\ued15", false, false),
            new Setting("About", string.Empty, stringResource.GetLocalized("Settings_About_Header"), stringResource.GetLocalized("Settings_About_Description"), "\ue946", false, false),
        };

        foreach (var setting in settings)
        {
            SettingsList.Add(new SettingViewModel(setting, this));
        }

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
        };
    }

    [RelayCommand]
    public void BreadcrumbBarItemClicked(BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
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
