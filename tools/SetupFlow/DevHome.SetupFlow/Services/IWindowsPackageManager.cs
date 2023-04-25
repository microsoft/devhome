// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
    /// Gets a value indicating whether the WindowsPackageManager COM server
    /// can be used to perform out-of-proc COM calls
    /// </summary>
    /// <returns>True if COM server is available</returns>
    public bool IsCOMServerAvailable
    {
        get;
    }

    public Task CheckForAppInstallerUpdateAsync();

    public Task UpdateAppInstallerAsync();

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
}
