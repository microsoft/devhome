// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.UITest.Configurations;

/// <summary>
/// Class model of the appsettings JSON file root object
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Gets or sets the target Dev Home package family name (e.g. Release, Canary, Dev)
    /// </summary>
    public string PackageFamilyName { get; set; }

    /// <summary>
    /// Gets or sets the windows application driver url
    /// </summary>
    public string WindowsApplicationDriverUrl { get; set; }

    /// <summary>
    /// Gets or sets the widget configuration
    /// </summary>
    public WidgetConfiguration Widget { get; set; }
}
