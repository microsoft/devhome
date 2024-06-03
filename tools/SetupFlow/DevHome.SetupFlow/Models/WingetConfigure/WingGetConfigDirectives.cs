// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Represents the directives in the WinGet config file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WingGetConfigDirectives
{
    public const string SecurityContextCurrent = "current";
    public const string SecurityContextElevated = "elevated";

    public string Description { get; set; }

    public bool AllowPrerelease { get; set; }

    public string Module { get; set; }

    /// <summary>
    /// Gets or sets SecurityContext required by the app installer.
    /// SecurityContext can be "current" or "elevated".
    /// If set to "elevated", the WinGet will request elevation and will run app installer from
    /// an elevated process.
    /// </summary>
    public string SecurityContext { get; set; } = SecurityContextCurrent;
}
