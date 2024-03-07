// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Properties of the WinGet configuration file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WinGetConfigProperties
{
    public WinGetConfigAssertion[] Assertions { get; set; }

    public WinGetConfigResource[] Resources { get; set; }

    public string ConfigurationVersion { get; set; }
}
