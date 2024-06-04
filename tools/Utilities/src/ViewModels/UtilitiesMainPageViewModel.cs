// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.Telemetry;
using DevHome.Utilities.TelemetryEvents;
using Windows.ApplicationModel;

namespace DevHome.Utilities.ViewModels;

public partial class UtilitiesMainPageViewModel : ObservableObject
{
    public ObservableCollection<UtilityViewModel> Utilities { get; set; }

    public UtilitiesMainPageViewModel(IExperimentationService experimentationService)
    {
        var appExAliasAbsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"Microsoft\\WindowsApps\\{Package.Current.Id.FamilyName}");
        var stringResource = new StringResource("DevHome.Utilities.pri", "DevHome.Utilities/Resources");

        Utilities = new ObservableCollection<UtilityViewModel>
        {
            new(Path.Combine(appExAliasAbsFolderPath, "DevHome.HostsFileEditorApp.exe"))
            {
                Title = stringResource.GetLocalized("HostsFileEditorUtilityTitle"),
                Description = stringResource.GetLocalized("HostsFileEditorUtilityDesc"),
                NavigateUri = "https://go.microsoft.com/fwlink/?Linkid=2271355",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\HostsUILib", "Hosts.ico"),
                SupportsLaunchAsAdmin = Microsoft.UI.Xaml.Visibility.Visible,
            },
            new(Path.Combine(appExAliasAbsFolderPath, "DevHome.RegistryPreviewApp.exe"))
            {
                Title = stringResource.GetLocalized("RegistryPreviewUtilityTitle"),
                Description = stringResource.GetLocalized("RegistryPreviewUtilityDesc"),
                NavigateUri = "https://go.microsoft.com/fwlink/?Linkid=2270966",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\RegistryPreview", "RegistryPreview.ico"),
                SupportsLaunchAsAdmin = Microsoft.UI.Xaml.Visibility.Collapsed,
            },
            new(Path.Combine(appExAliasAbsFolderPath, "DevHome.EnvironmentVariablesApp.exe"))
            {
                Title = stringResource.GetLocalized("EnvVariablesEditorUtilityTitle"),
                Description = stringResource.GetLocalized("EnvVariablesEditorUtilityDesc"),
                NavigateUri = "https://go.microsoft.com/fwlink/?Linkid=2270894",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\EnvironmentVariables", "EnvironmentVariables.ico"),
                SupportsLaunchAsAdmin = Microsoft.UI.Xaml.Visibility.Visible,
            },
            new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"Microsoft\\WindowsApps\\{Package.Current.Id.FamilyName}\\devhome.pi.exe"), experimentationService, "ProjectIronsidesExperiment")
            {
                Title = stringResource.GetLocalized("ProjectIronsidesTitle"),
                Description = stringResource.GetLocalized("ProjectIronsidesDesc"),
                NavigateUri = "https://aka.ms/projectironsides",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "PI.ico"),
            },
        };

        TelemetryFactory.Get<ITelemetry>().Log("Utilities_UtilitiesMainPage", LogLevel.Critical, new UtilitiesMainPageViewModelEvent());
    }
}
