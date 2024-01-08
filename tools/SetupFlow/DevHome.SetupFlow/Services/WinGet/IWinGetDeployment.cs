// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services.WinGet;

internal interface IWinGetDeployment
{
    /// <summary>
    /// Check whether the WindowsPackageManagerServer is available to create
    /// out-of-proc COM objects
    /// </summary>
    /// <returns>True if COM Server is available, false otherwise</returns>
    public Task<bool> IsAvailableAsync();

    /// <summary>
    /// Checks if AppInstaller has an available update
    /// </summary>
    /// <returns>True if an AppInstaller update is available, false otherwise</returns>
    public Task<bool> IsUpdateAvailableAsync();

    /// <summary>
    /// Register AppInstaller
    /// </summary>
    /// <returns>True if AppInstaller was registered, false otherwise.</returns>
    public Task<bool> RegisterAppInstallerAsync();

    /// <summary>
    /// Check if configuration is stubbed
    /// </summary>
    /// <returns>True if configuration is stubbed, false otherwise</returns>
    public Task<bool> IsConfigurationStubbedAsync();

    /// <summary>
    /// Unstub configuration
    /// </summary>
    /// <returns>True if configuration was unstubbed, false otherwise</returns>
    public Task<bool> UnstubConfigurationAsync();
}
