// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using Windows.ApplicationModel;

namespace DevHome.Utilities.ViewModels;

public partial class UtilitiesMainPageViewModel : ObservableObject
{
    public ObservableCollection<UtilityViewModel> Utilities { get; set; }

    public UtilitiesMainPageViewModel(IExperimentationService experimentationService)
    {
        var stringResource = new StringResource("DevHome.Utilities.pri", "DevHome.Utilities/Resources");

        Utilities = new ObservableCollection<UtilityViewModel>
        {
            new("DevHome.HostsFileEditorApp.exe")
            {
                Title = stringResource.GetLocalized("HostsFileEditorUtilityTitle"),
                Description = stringResource.GetLocalized("HostsFileEditorUtilityDesc"),
                NavigateUri = "https://aka.ms/PowerToysOverview_HostsFileEditor",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\HostsUILib", "Hosts.ico"),
            },
            new("DevHome.RegistryPreviewApp.exe")
            {
                Title = stringResource.GetLocalized("RegistryPreviewUtilityTitle"),
                Description = stringResource.GetLocalized("RegistryPreviewUtilityDesc"),
                NavigateUri = "https://aka.ms/PowerToysOverview_RegistryPreview",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\RegistryPreview", "RegistryPreview.ico"),
            },
            new("DevHome.EnvironmentVariablesApp.exe")
            {
                Title = stringResource.GetLocalized("EnvVariablesEditorUtilityTitle"),
                Description = stringResource.GetLocalized("EnvVariablesEditorUtilityDesc"),
                NavigateUri = "https://aka.ms/PowerToysOverview_EnvironmentVariables",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\EnvironmentVariables", "EnvironmentVariables.ico"),
            },
            new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"Microsoft\\WindowsApps\\{Package.Current.Id.FamilyName}\\devhome.pi.exe"), experimentationService, "ProjectIronsidesExperiment")
            {
                Title = stringResource.GetLocalized("ProjectIronsidesTitle"),
                Description = stringResource.GetLocalized("ProjectIronsidesDesc"),
                NavigateUri = "https://aka.ms/projectironsides",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "PI.ico"),
            },
        };
    }
}
