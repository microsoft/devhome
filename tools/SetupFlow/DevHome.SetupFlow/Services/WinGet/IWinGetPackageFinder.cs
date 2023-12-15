// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Services.WinGet;
public interface IWinGetPackageFinder
{
    /// <summary>
    /// Search for packages in the provided catalog
    /// </summary>
    /// <param name="catalog">Catalog from where the packages are queried</param>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of packages</returns>
    public Task<IList<IWinGetPackage>> SearchAsync(WinGetCatalog catalog, string query, uint limit = 0);

    /// <summary>
    /// Get packages from the provided catalog
    /// </summary>
    /// <param name="catalog">Catalog from where the packages are queried</param>
    /// <param name="packageIds">Set of package ids</param>
    /// <returns>List of packages</returns>
    public Task<IList<IWinGetPackage>> GetPackagesAsync(WinGetCatalog catalog, ISet<string> packageIds);
}
