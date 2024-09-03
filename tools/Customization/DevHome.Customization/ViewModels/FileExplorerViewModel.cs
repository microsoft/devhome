// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Helpers;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using DevHome.FileExplorerSourceControlIntegration.Services;
using FileExplorerSourceControlIntegration;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Storage;

namespace DevHome.Customization.ViewModels;

public partial class FileExplorerViewModel : ObservableObject
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
        RefreshTrackedRepositories();
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

    public bool IsVersionControlIntegrationEnabled
    {
        get => CalculateEnabled("VersionControlIntegration");
        set => OnToggledVersionControlIntegrationSettingAsync(value);
    }

    public bool ShowVersionControlInformation
    {
        get => CalculateEnabled("ShowVersionControlInformation");
        set => OnToggledVersionControlInformationSettingAsync(value);
    }

    public bool ShowRepositoryStatus
    {
        get => CalculateEnabled("ShowRepositoryStatus");
        set => OnToggledRepositoryStatusSettingAsync(value);
    }

    [RelayCommand]
    public async Task AddFolderClick()
    {
        if (IsFeatureEnabled)
        {
            await Task.Run(async () =>
            {
                using var folderDialog = new WindowOpenFolderDialog();
                StorageFolder? repoRootfolder = null;

                try
                {
                    repoRootfolder = await folderDialog.ShowAsync(Application.Current.GetService<Window>());
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error occured when selecting a folder for adding a repository.");
                }

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

    public async void AssignSourceControlProviderToRepository(IExtensionWrapper? extension, string rootPath)
    {
        await Task.Run(() =>
        {
            var extensionCLSID = extension?.ExtensionClassId ?? string.Empty;
            var result = SourceControlIntegration.ValidateSourceControlExtension(extensionCLSID, rootPath);
            if (result.Result == ResultType.Failure)
            {
                _log.Error("Failed to validate source control extension");
                return;
            }

            try
            {
                var wrapperResult = ExtraFolderPropertiesWrapper.Register(rootPath, typeof(SourceControlProvider).GUID);
                if (!wrapperResult.Succeeded)
                {
                    _log.Error(wrapperResult.ExtendedError, "Failed to register folder for source control integration");
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "An exception occurred while registering folder for File Explorer source control integration");
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

    public async void OnToggledVersionControlIntegrationSettingAsync(bool value)
    {
        await LocalSettingsService!.SaveSettingAsync("VersionControlIntegration", value);

        if (!value)
        {
            _log.Information("The user has disabled version control integration inside Dev Home");
            ExtraFolderPropertiesWrapper.UnregisterAllForCurrentApp();
            _log.Information("Unregistered all repositories in File Explorer as setting is disabled");
        }
        else
        {
            _log.Information("The user has enabled version control integration in Dev Home.");
            var repoCollection = RepoTracker.GetAllTrackedRepositories();
            foreach (var repo in repoCollection)
            {
                ExtraFolderPropertiesWrapper.Register(repo.Key, typeof(SourceControlProvider).GUID);
            }

            _log.Information("Dev Home has restored registration for enhanced repositories it is aware about");
        }
    }

    public async void OnToggledVersionControlInformationSettingAsync(bool value)
    {
        if (!value)
        {
            _log.Information("The user has disabled display of version control information in File Explorer");
        }

        await LocalSettingsService!.SaveSettingAsync("ShowVersionControlInformation", value);
    }

    public async void OnToggledRepositoryStatusSettingAsync(bool value)
    {
        if (!value)
        {
            _log.Information("The user has disabled display or repository status in File Explorer");
        }

        await LocalSettingsService!.SaveSettingAsync("ShowRepositoryStatus", value);
    }
}
