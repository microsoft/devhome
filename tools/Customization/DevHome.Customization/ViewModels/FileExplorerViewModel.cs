// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Helpers;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using DevHome.FileExplorerSourceControlIntegration.Services;
using DevHome.Services;
using DevHome.Telemetry;
using FileExplorerSourceControlIntegration;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinUIEx;

namespace DevHome.Customization.ViewModels;

public partial class FileExplorerViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly ShellSettings _shellSettings;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<RepositoryInformation> TrackedRepositories { get; } = new();

    private RepositoryTracking RepoTracker { get; set; } = new(null);

    private readonly string _unassigned = "00000000-0000-0000-0000-000000000000";

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(FileExplorerViewModel));

    public IExperimentationService ExperimentationService { get; }

    public IExtensionService ExtensionService { get; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public bool IsFeatureEnabled => ExperimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration") && ExtraFolderPropertiesWrapper.IsSupported();

    [ObservableProperty]
    private bool _isVersionControlIntegrationEnabled;

    [ObservableProperty]
    private bool _showVersionControlInformation;

    [ObservableProperty]
    private bool _showRepositoryStatus;

    public bool IsFileExplorerIntegrationSettingOn { get; set; }

    public FileExplorerViewModel(IExperimentationService experimentationService, IExtensionService extensionService, ILocalSettingsService localSettingsService)
    {
        _shellSettings = new ShellSettings();
        ExperimentationService = experimentationService;
        ExtensionService = extensionService;
        LocalSettingsService = localSettingsService;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("FileExplorer_Header"), typeof(FileExplorerViewModel).FullName!)
        ];
        LoadFileExplorerSettings();
        RefreshTrackedRepositories();
    }

    public void LoadFileExplorerSettings()
    {
        if (ExperimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            IsVersionControlIntegrationEnabled = CalculateEnabled("VersionControlIntegration");
            ShowVersionControlInformation = CalculateEnabled("ShowVersionControlInformation");
            ShowRepositoryStatus = CalculateEnabled("ShowRepositoryStatus");
            IsFileExplorerIntegrationSettingOn = true;
        }
    }

    public void RefreshTrackedRepositories()
    {
        if (ExperimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            TrackedRepositories.Clear();
            var repoCollection = RepoTracker.GetAllTrackedRepositories();
            foreach (KeyValuePair<string, string> data in repoCollection)
            {
                TrackedRepositories.Add(new RepositoryInformation(data.Key, data.Value));
            }
        }
    }

    public bool ShowFileExtensions
    {
        get => FileExplorerSettings.ShowFileExtensionsEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFileExtensions", value.ToString());
            FileExplorerSettings.SetShowFileExtensionsEnabled(value);
        }
    }

    public bool ShowHiddenAndSystemFiles
    {
        get => FileExplorerSettings.ShowHiddenAndSystemFilesEnabled();
        set
        {
            SettingChangedEvent.Log("ShowHiddenAndSystemFiles", value.ToString());
            FileExplorerSettings.SetShowHiddenAndSystemFilesEnabled(value);
        }
    }

    public bool ShowFullPathInTitleBar
    {
        get => FileExplorerSettings.ShowFullPathInTitleBarEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFullPathInTitleBar", value.ToString());
            FileExplorerSettings.SetShowFullPathInTitleBarEnabled(value);
        }
    }

    public bool ShowEmptyDrives
    {
        get => _shellSettings.ShowEmptyDrivesEnabled();
        set
        {
            SettingChangedEvent.Log("ShowEmptyDrives", value.ToString());
            _shellSettings.SetShowEmptyDrivesEnabled(value);
        }
    }

    public bool ShowFilesAfterExtraction
    {
        get => _shellSettings.ShowFilesAfterExtractionEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFilesAfterExtraction", value.ToString());
            _shellSettings.SetShowFilesAfterExtractionEnabled(value);
        }
    }

    public bool EndTaskOnTaskBarEnabled
    {
        get => _shellSettings.EndTaskOnTaskBarEnabled();
        set
        {
            SettingChangedEvent.Log("EndTaskOnTaskBarEnabled", value.ToString());
            _shellSettings.SetEndTaskOnTaskBarEnabled(value);
        }
    }

    [RelayCommand]
    public async Task AddFolderClick()
    {
        if (IsFeatureEnabled)
        {
            await Task.Run(async () =>
            {
                using var folderDialog = new WindowOpenFolderDialog();
                var repoRootfolder = await folderDialog.ShowAsync(Application.Current.GetService<Window>());
                if (repoRootfolder != null && repoRootfolder.Path.Length > 0)
                {
                    _log.Information($"Selected '{repoRootfolder.Path}' as location to register");
                    RepoTracker.AddRepositoryPath(_unassigned, repoRootfolder.Path);
                }
                else
                {
                    _log.Information("Didn't select a location to register");
                }
            });
            RefreshTrackedRepositories();
        }
    }

    public void RemoveTrackedRepositoryFromDevHome(string rootPath)
    {
        ExtraFolderPropertiesWrapper.Unregister(rootPath);
        RepoTracker.RemoveRepositoryPath(rootPath);
        RefreshTrackedRepositories();
    }

    public async void AssignSourceControlProviderToRepository(string extensionName, string rootPath)
    {
        await Task.Run(async () =>
        {
            var sourceControlExtensions = await ExtensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository);
            var extensionCLSID = sourceControlExtensions.FirstOrDefault(extension => extension.ExtensionDisplayName == extensionName)?.ExtensionClassId ?? string.Empty;
            var result = SourceControlIntegration.ValidateSourceControlExtension(extensionCLSID, rootPath);
            if (result.Result == ResultType.Failure)
            {
                _log.Error("Failed to validate source control extension");
                return;
            }

            var wrapperResult = ExtraFolderPropertiesWrapper.Register(rootPath, typeof(SourceControlProvider).GUID);
            if (!wrapperResult.Succeeded)
            {
                _log.Error(wrapperResult.ExtendedError, "Failed to register folder for source control integration");
                return;
            }

            RepoTracker.ModifySourceControlProviderForTrackedRepository(extensionCLSID, rootPath);
        });
        RefreshTrackedRepositories();
    }

    public bool CalculateEnabled(string settingName)
    {
        if (LocalSettingsService!.HasSettingAsync(settingName).Result)
        {
            return LocalSettingsService.ReadSettingAsync<bool>(settingName).Result;
        }

        // Settings disabled by default
        return false;
    }

    [RelayCommand]
    public async Task OnToggledVersionControlIntegrationSettingAsync()
    {
        IsVersionControlIntegrationEnabled = !IsVersionControlIntegrationEnabled;
        await LocalSettingsService!.SaveSettingAsync("VersionControlIntegration", IsVersionControlIntegrationEnabled);
        _log.Information("Saved FE Enable setting");

        if (!IsVersionControlIntegrationEnabled)
        {
            _log.Information("Saved FE Enable setting: false");
            IsFileExplorerIntegrationSettingOn = false;
        }
        else
        {
            IsFileExplorerIntegrationSettingOn = true;
        }
    }

    [RelayCommand]
    public async Task OnToggledVersionControlInformationSettingAsync()
    {
        ShowVersionControlInformation = !ShowVersionControlInformation;
        await LocalSettingsService!.SaveSettingAsync("ShowVersionControlInformation", ShowVersionControlInformation);
    }

    [RelayCommand]
    public async Task OnToggledRepositoryStatusSettingAsync()
    {
        ShowRepositoryStatus = !ShowRepositoryStatus;
        await LocalSettingsService!.SaveSettingAsync("ShowRepositoryStatus", ShowRepositoryStatus);
    }
}
