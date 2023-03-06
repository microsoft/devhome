// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Models;

namespace DevHome.SetupFlow.AppManagement.Services;

/// <summary>
/// Interface for interacting with the WinGet package manager.
/// More details: https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </summary>
public interface IWindowsPackageManager
{
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
    /// Gets a composite catalog for the predefined <c>msstore</c> and local catalogs.
    /// </summary>
    public IWinGetCatalog MsStoreCatalog
    {
        get;
    }

    /// <summary>
    /// Install a winget package
    /// </summary>
    /// <param name="package">Package to install</param>
    public Task InstallPackageAsync(WinGetPackage package);
}
