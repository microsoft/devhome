// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Base for the settings of a WinGet config resource.
/// Ensure is not included because it may or may not be handled by the resource
/// But is a fundamental concept of Dsc resources so it is included in the base.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public abstract class WinGetConfigSettingsBase
{
    public string Ensure { get; set; }
}
