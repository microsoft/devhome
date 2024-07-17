// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using DevHome.Customization.Views;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Storage;
using Windows.UI.ViewManagement.Core;
using WinUIEx;

namespace DevHome.Customization.ViewModels;

public partial class FileExplorerViewModel : ObservableObject
{
    private readonly ShellSettings _shellSettings;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<string> TrackedRepositories { get; } = new();

    private RepositoryTracking RepoTracker { get; set; } = new(null);

    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(FileExplorerViewModel));

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
        await Task.Run(async () =>
        {
            using var folderDialog = new WindowOpenFolderDialog();
            var repoRootfolder = await folderDialog.ShowAsync(Application.Current.GetService<WindowEx>());
            if (repoRootfolder != null && repoRootfolder.Path.Length > 0)
            {
                log.Information($"Selected '{repoRootfolder.Path}' as location to register");
            }
            else
            {
                log.Information("Didn't select a location to register");
            }
        });
    }

    public void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
    {
    }

    public ICommand? SourceControlProviderSelection_Click()
    {
        return null;
    }
}
