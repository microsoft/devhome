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
                UtilityAutomationId = "DevHome.HostsFileEditor",
            },
            new(Path.Combine(appExAliasAbsFolderPath, "DevHome.RegistryPreviewApp.exe"))
            {
                Title = stringResource.GetLocalized("RegistryPreviewUtilityTitle"),
                Description = stringResource.GetLocalized("RegistryPreviewUtilityDesc"),
                NavigateUri = "https://go.microsoft.com/fwlink/?Linkid=2270966",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\RegistryPreview", "RegistryPreview.ico"),
                SupportsLaunchAsAdmin = Microsoft.UI.Xaml.Visibility.Collapsed,
                UtilityAutomationId = "DevHome.RegistryPreview",
            },
            new(Path.Combine(appExAliasAbsFolderPath, "DevHome.EnvironmentVariablesApp.exe"))
            {
                Title = stringResource.GetLocalized("EnvVariablesEditorUtilityTitle"),
                Description = stringResource.GetLocalized("EnvVariablesEditorUtilityDesc"),
                NavigateUri = "https://go.microsoft.com/fwlink/?Linkid=2270894",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "Assets\\EnvironmentVariables", "EnvironmentVariables.ico"),
                SupportsLaunchAsAdmin = Microsoft.UI.Xaml.Visibility.Visible,
                UtilityAutomationId = "DevHome.EnvironmentVariables",
            },
            new(Path.Combine(appExAliasAbsFolderPath, "DevHome.DevInsights.exe"))
            {
                Title = stringResource.GetLocalized("DevInsightsTitle"),
                Description = stringResource.GetLocalized("DevInsightsDesc"),
                NavigateUri = "https://go.microsoft.com/fwlink/?linkid=2275140",
                ImageSource = Path.Combine(AppContext.BaseDirectory, "DI.ico"),
                UtilityAutomationId = "DevHome.DevInsights",
            },
        };

        TelemetryFactory.Get<ITelemetry>().Log("Utilities_UtilitiesMainPage", LogLevel.Critical, new UtilitiesMainPageViewModelEvent());
    }
}
