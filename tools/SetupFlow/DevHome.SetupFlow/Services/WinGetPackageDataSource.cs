// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;
using DevHome.SetupFlow.Models;
using Serilog;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Abstract class for a WinGet package data source
/// </summary>
public abstract class WinGetPackageDataSource
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WinGetPackageDataSource));

    /// <summary>
    /// Gets the total number of package catalogs available in this data source
    /// </summary>
    public abstract int CatalogCount { get; }

    protected IWinGet WinGet { get; }

    public WinGetPackageDataSource(IWinGet winget)
    {
        WinGet = winget;
    }

    /// <summary>
    /// Initialize the data source
    /// </summary>
    public abstract Task InitializeAsync();

    /// <summary>
    /// Load catalogs from the data source
    /// </summary>
    /// <returns>List of package catalogs</returns>
    public abstract Task<IList<PackageCatalog>> LoadCatalogsAsync();

    /// <summary>
    /// Get a list of packages from WinGet catalog
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <param name="packageUris">List of package URIs</param>
    /// <returns>List of packages</returns>
    protected async Task<IList<IWinGetPackage>> GetPackagesAsync(IList<WinGetPackageUri> packageUris)
    {
        List<IWinGetPackage> result = new();

        // Skip search if package data source is empty
        if (!packageUris.Any())
        {
            _log.Warning($"{nameof(GetPackagesAsync)} received an empty list of items. Skipping search.");
            return result;
        }

        // Get packages from winget catalog
        return await WinGet.GetPackagesAsync(packageUris);
    }
}
