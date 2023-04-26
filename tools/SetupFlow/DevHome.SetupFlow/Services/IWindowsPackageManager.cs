// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Interface for interacting with the WinGet package manager.
/// More details: https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </summary>
public interface IWindowsPackageManager
{
    /// <summary>
    /// Gets the predefined WinGet catalog id
    /// </summary>
    public string WinGetCatalogId
    {
        get;
    }

    /// <summary>
    /// Gets the predefined MsStore catalog id
    /// </summary>
    public string MsStoreId
    {
        get;
    }

    /// <summary>
    /// Gets a composite catalog for all remote and local catalogs.
    /// </summary>
    public IWinGetCatalog AllCatalogs
    {
        get;
    }

    /// <summary>
    /// Gets a composite catalog for the predefined <c>winget</c> and local catalogs.
    /// </summary>
    public IWinGetCatalog WinGetCatalog
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether the WindowsPackageManagerServer is available to create out-of-proc COM objects
    /// </summary>
    /// <returns>True if COM Server is available, false otherwise</returns>
    public bool IsCOMServerAvailable
    {
        get;
    }

    /// <summary>
    /// Opens all custom composite catalogs.
    /// </summary>
    /// <exception cref="CatalogConnectionException">Exception thrown if a catalog connection failed</exception>
    public Task ConnectToAllCatalogsAsync();

    /// <summary>
    /// Install a winget package
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <returns>Install package result</returns>
    public Task<InstallPackageResult> InstallPackageAsync(WinGetPackage package);

    /// <summary>
    /// Checks if AppInstaller has an available update
    /// </summary>
    /// <param name="forceCheck">True to force re-evaluating the update availability value. False to use the last known value..</param>
    /// <returns>True if an AppInstaller update is available, false otherwise</returns>
    public Task<bool> IsAppInstallerUpdateAvailableAsync(bool forceCheck = false);

    /// <summary>
    /// Start AppInstaller update
    /// </summary>
    /// <returns>True if the update started, false otherwise.</returns>
    public Task<bool> StartAppInstallerUpdateAsync();

    /// <summary>
    /// Occurrs when AppInstaller update has completed
    /// </summary>
    public event EventHandler AppInstallerUpdateCompleted;
}
