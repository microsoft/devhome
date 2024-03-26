// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Customization.Models;
using DevHome.Customization.TelemetryEvents;
using Microsoft.Internal.Windows.DevHome.Helpers;

namespace DevHome.Customization.ViewModels;

public partial class DeveloperFileExplorerViewModel : ObservableObject
{
    private readonly ShellSettings _shellSettings;

    public DeveloperFileExplorerViewModel()
    {
        _shellSettings = new ShellSettings();
    }

    public bool ShowFileExtensions
    {
        get => DeveloperFileExplorerSettings.ShowFileExtensionsEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFileExtensions", value.ToString());
            DeveloperFileExplorerSettings.SetShowFileExtensionsEnabled(value);
        }
    }

    public bool ShowHiddenAndSystemFiles
    {
        get => DeveloperFileExplorerSettings.ShowHiddenAndSystemFilesEnabled();
        set
        {
            SettingChangedEvent.Log("ShowHiddenAndSystemFiles", value.ToString());
            DeveloperFileExplorerSettings.SetShowHiddenAndSystemFilesEnabled(value);
        }
    }

    public bool ShowFullPathInTitleBar
    {
        get => DeveloperFileExplorerSettings.ShowFullPathInTitleBarEnabled();
        set
        {
            SettingChangedEvent.Log("ShowFullPathInTitleBar", value.ToString());
            DeveloperFileExplorerSettings.SetShowFullPathInTitleBarEnabled(value);
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
