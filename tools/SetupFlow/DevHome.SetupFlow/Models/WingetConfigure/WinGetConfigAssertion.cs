// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Represents the assertions in the WinGet config file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WinGetConfigAssertion
{
    public string Resource { get; set; }

    public WingGetConfigDirectives Directives { get; set; }

    public WinGetConfigSettingsBase Settings { get; set; }
}
