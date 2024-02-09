// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Services.WinGet;

internal interface IWinGetPackageFinder
{
    /// <summary>
    /// Search for packages in the provided catalog
    /// </summary>
    /// <param name="catalog">Catalog from where the packages are queried</param>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of packages</returns>
    public Task<IList<CatalogPackage>> SearchAsync(WinGetCatalog catalog, string query, uint limit);

    /// <summary>
    /// Get packages from the provided catalog
    /// </summary>
    /// <param name="catalog">Catalog from where the packages are queried</param>
    /// <param name="packageIds">Set of package ids</param>
    /// <returns>List of packages found</returns>
    public Task<IList<CatalogPackage>> GetPackagesAsync(WinGetCatalog catalog, ISet<string> packageIds);

    /// <summary>
    /// Get a single package from the provided catalog
    /// </summary>
    /// <param name="catalog">Catalog from where the package is queried</param>
    /// <param name="packageId">Package id</param>
    /// <returns>Package. Or <see langword="null"/> if not found.</returns>
    public Task<CatalogPackage> GetPackageAsync(WinGetCatalog catalog, string packageId);

    /// <summary>
    /// Check if the package requires elevation
    /// </summary>
    /// <param name="package">Package to check</param>
    /// <returns>True if the package requires elevation</returns>
    public bool IsElevationRequired(CatalogPackage package);
}
