// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;

namespace DevHome.Settings.Models;
public class Setting
{
    public string? LinkName { get; }

    public string? Header { get; }

    public string? Description { get; }

    public Setting(string linkName, string header, string description)
    {
        LinkName = linkName;
        Header = header;
        Description = description;
    }

    public string GetSettingsCard()
    {
        var settingsCardString = $@"<labs:SettingsCard xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:labs=""using:CommunityToolkit.Labs.WinUI"" x:Uid=""ms-resource:///DevHome.Settings/Resources/Settings_{LinkName}"" IsClickEnabled=""True"" Command=""{{x:Bind ViewModel.NavigateSettingsPreferencesCommand}}"" />";
        return settingsCardString;
    }
}
