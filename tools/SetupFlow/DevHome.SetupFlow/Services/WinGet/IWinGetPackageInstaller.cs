// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Services.WinGet;

public interface IWinGetPackageInstaller
{
    /// <summary>
    /// Install a package from WinGet catalog
    /// </summary>
    /// <param name="catalog">Catalog from which to install the package</param>
    /// <param name="packageId">Package id to install</param>
    /// <returns>Result of the installation</returns>
    public Task<InstallPackageResult> InstallPackageAsync(WinGetCatalog catalog, string packageId);

    /// <summary>
    /// Check if the package requires elevation
    /// </summary>
    /// <param name="package">Package to check</param>
    /// <returns>True if the package requires elevation</returns>
    public bool IsElevationRequired(CatalogPackage package);
}
