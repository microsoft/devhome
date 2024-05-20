// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.HostsFileEditor.ViewModels;
using HostsUILib.Settings;

namespace DevHome.HostsFileEditor.Helpers;

public class UserSettings : IUserSettings
{
    public bool LoopbackDuplicates
    {
        get => SettingsViewModel.LoopbackDuplicates;
        set => SettingsViewModel.LoopbackDuplicates = value;
    }

    public HostsAdditionalLinesPosition AdditionalLinesPosition
    {
        get => (HostsAdditionalLinesPosition)SettingsViewModel.AdditionalLinesPosition;
        set => SettingsViewModel.AdditionalLinesPosition = (int)value;
    }

    public HostsEncoding Encoding
    {
        get => (HostsEncoding)SettingsViewModel.Encoding;
        set => SettingsViewModel.Encoding = (int)value;
    }

    public bool ShowStartupWarning
    {
        get => SettingsViewModel.ShowStartupWarning;
        set => SettingsViewModel.ShowStartupWarning = value;
    }

    private HostsFileEditorSettingsViewModel SettingsViewModel { get; }

#pragma warning disable CS0067 // The event 'UserSettings.LoopbackDuplicatesChanged' is never used
    public event EventHandler LoopbackDuplicatesChanged;
#pragma warning restore CS0067 // The event 'UserSettings.LoopbackDuplicatesChanged' is never used

    public UserSettings()
    {
        SettingsViewModel = HostsFileEditorApp.GetService<HostsFileEditorSettingsViewModel>();

        ShowStartupWarning = SettingsViewModel.ShowStartupWarning;
        Encoding = (HostsEncoding)SettingsViewModel.Encoding;
        AdditionalLinesPosition = (HostsAdditionalLinesPosition)SettingsViewModel.AdditionalLinesPosition;
        LoopbackDuplicates = SettingsViewModel.LoopbackDuplicates;
    }
}
