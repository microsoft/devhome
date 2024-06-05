// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Represents a resource block in the WinGet config file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WinGetConfigResource
{
    public string Resource { get; set; }

    public WingGetConfigDirectives Directives { get; set; }

    public WinGetConfigSettingsBase Settings { get; set; }

    public string Id { get; set; }

    public string[] DependsOn { get; set; }
}
