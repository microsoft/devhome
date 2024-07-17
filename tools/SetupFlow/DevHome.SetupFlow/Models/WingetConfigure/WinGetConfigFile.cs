// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// The properties of the WinGet config file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WinGetConfigFile
{
    public WinGetConfigProperties Properties { get; set; }
}
