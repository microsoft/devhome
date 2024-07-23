// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Helpers;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinUIEx;

namespace DevHome.Customization.ViewModels;

public partial class FileExplorerViewModel : ObservableObject
{
    private readonly ShellSettings _shellSettings;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<RepositoryInformation> TrackedRepositories { get; } = new();

    private RepositoryTracking RepoTracker { get; set; } = new(null);

    private readonly string unassigned = "00000000-0000-0000-0000-000000000000";

    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(FileExplorerViewModel));

    private readonly IExperimentationService experimentationService = Application.Current.GetService<IExperimentationService>();

    public FileExplorerViewModel()
    {
        _shellSettings = new ShellSettings();

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
        if (experimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
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

    public async void AddFolderButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        if (experimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            await Task.Run(async () =>
            {
                using var folderDialog = new WindowOpenFolderDialog();
                var repoRootfolder = await folderDialog.ShowAsync(Application.Current.GetService<WindowEx>());
                if (repoRootfolder != null && repoRootfolder.Path.Length > 0)
                {
                    log.Information($"Selected '{repoRootfolder.Path}' as location to register");
                    RepoTracker.AddRepositoryPath(unassigned, repoRootfolder.Path);
                }
                else
                {
                    log.Information("Didn't select a location to register");
                }
            });
            RefreshTrackedRepositories();
        }
    }

    public void RemoveTrackedRepositoryFromDevHome(string rootPath)
    {
        RepoTracker.RemoveRepositoryPath(rootPath);
        RefreshTrackedRepositories();
    }

    public async void AssignSourceControlProviderToRepository(string extensionName, string rootPath)
    {
        await Task.Run(async () =>
        {
            var extensionService = Application.Current.GetService<IExtensionService>();
            var sourceControlExtensions = await extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository);
            var extensionCLSID = sourceControlExtensions.FirstOrDefault(extension => extension.ExtensionDisplayName == extensionName)?.ExtensionClassId ?? string.Empty;
            var result = SourceControlIntegration.ValidateSourceControlExtension(extensionCLSID, rootPath);
            if (result.Result == ResultType.Failure)
            {
                log.Error("Failed to validate source control extension");
                return;
            }

            RepoTracker.ModifySourceControlProviderForTrackedRepository(extensionCLSID, rootPath);
        });
        RefreshTrackedRepositories();
    }
}
