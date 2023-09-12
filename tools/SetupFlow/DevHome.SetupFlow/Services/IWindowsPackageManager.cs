// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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
    /// Opens all custom composite catalogs.
    /// </summary>
    /// <param name="force">Force connect</param>
    /// <exception cref="CatalogConnectionException">Exception thrown if a catalog connection failed</exception>
    public Task ConnectToAllCatalogsAsync(bool force = false);

    /// <summary>
    /// Install a winget package
    /// </summary>
    /// <param name="package">Package to install</param>
    /// <param name="activityId">Guid to correlate this task to the setupflow activity.</param>
    /// <returns>Install package result</returns>
    public Task<InstallPackageResult> InstallPackageAsync(WinGetPackage package, Guid activityId);

    /// <summary>
    /// Checks if AppInstaller has an available update
    /// </summary>
    /// <returns>True if an AppInstaller update is available, false otherwise</returns>
    public Task<bool> IsAppInstallerUpdateAvailableAsync();

    /// <summary>
    /// Start AppInstaller update
    /// </summary>
    /// <returns>True if the update started, false otherwise.</returns>
    public Task<bool> StartAppInstallerUpdateAsync();

    /// <summary>
    /// Check whether the WindowsPackageManagerServer is available to create
    /// out-of-proc COM objects
    /// </summary>
    /// <returns>True if COM Server is available, false otherwise</returns>
    public Task<bool> IsCOMServerAvailableAsync();

    /// <summary>
    /// Register AppInstaller
    /// </summary>
    /// <returns>True if AppInstaller was registered, false otherwise.</returns>
    public Task<bool> RegisterAppInstallerAsync();

    /// <summary>
    /// Get packages from a set of package uri.
    /// </summary>
    /// <param name="packageUriSet">Set of package uri</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="FindPackagesException">Exception thrown if the get packages operation failed</exception>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet);
}
