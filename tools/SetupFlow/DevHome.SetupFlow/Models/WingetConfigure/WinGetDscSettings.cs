// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Represents the settings for a WinGetDsc resource.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WinGetDscSettings : WinGetConfigSettingsBase
{
    public string Id { get; set; }

    public string Source { get; set; }
}
