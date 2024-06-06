// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Customization.ViewModels;

public partial class FileExplorerViewModel : ObservableObject
{
    private readonly ShellSettings _shellSettings;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<string> TrackedRepositories { get; } = new();

    private RepositoryTracking RepoTracker { get; set; } = new(null);

    public ObservableCollection<FileExplorerSourceControlIntegrationViewModel> LocalRepositoryProviders { get; } = new();

    public FileExplorerViewModel()
    {
        _shellSettings = new ShellSettings();

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("FileExplorer_Header"), typeof(FileExplorerViewModel).FullName!)
        ];

        var experimentationService = Application.Current.GetService<IExperimentationService>();
        if (experimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            var extensionService = Application.Current.GetService<IExtensionService>();
            var sourceControlExtensions = Task.Run(async () => await extensionService.GetInstalledExtensionsAsync(ProviderType.SourceControlIntegration)).Result.ToList();
            sourceControlExtensions.Sort((a, b) => string.Compare(a.ExtensionDisplayName, b.ExtensionDisplayName, System.StringComparison.OrdinalIgnoreCase));
            sourceControlExtensions.ForEach((sourceControlExtension) =>
            {
                LocalRepositoryProviders.Add(new FileExplorerSourceControlIntegrationViewModel(sourceControlExtension));
            });
        }

        RefreshTrackedRepositories();
    }

    public void RefreshTrackedRepositories()
    {
        var experimentationService = Application.Current.GetService<IExperimentationService>();
        if (experimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            TrackedRepositories.Clear();
            var repoCollection = RepoTracker.GetAllTrackedRepositories();
            foreach (KeyValuePair<string, string> data in repoCollection)
            {
                TrackedRepositories.Add(data.Key);
            }
        }
    }

    public void AddRepositoryPath(string extension, string rootPath)
    {
        var normalizedPath = rootPath.ToUpper(CultureInfo.InvariantCulture).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        RepoTracker.AddRepositoryPath(extension, normalizedPath);
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
}
