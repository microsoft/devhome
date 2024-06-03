// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.UITest.Configurations;

/// <summary>
/// Class model of the appsettings Widget object
/// </summary>
public class WidgetConfiguration
{
    /// <summary>
    /// Gets or sets the widget provider
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Gets or sets the automation id prefix of the dashboard widgets
    /// </summary>
    public string IdPrefix { get; set; }
}
