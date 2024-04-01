// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Setup flow configuration options defined in appsettings.json
/// </summary>
public class SetupFlowOptions
{
    public string StringResourceName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the string resource map path for the setup flow project
    /// </summary>
    public string StringResourcePath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the path for the WinGet package JSON data source
    /// </summary>
    public string WinGetPackageJsonDataSourcePath
    {
        get; set;
    }
}
