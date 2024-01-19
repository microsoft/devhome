// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Represents the directives in the WinGet config file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WingGetConfigDirectives
{
    public string Description { get; set; }

    public bool AllowPrerelease { get; set; }

    public string Module { get; set; }
}
