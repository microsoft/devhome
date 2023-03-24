// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Models;

namespace DevHome.SetupFlow.AppManagement.Services;

/// <summary>
/// Abstract class for a WinGet package data source
/// </summary>
public abstract class WinGetPackageDataSource
{
    private readonly IWindowsPackageManager _wpm;

    /// <summary>
    /// Gets the total number of package catalogs available in this data source
    /// </summary>
    public abstract int CatalogCount
    {
        get;
    }

    public WinGetPackageDataSource(IWindowsPackageManager wpm)
    {
        _wpm = wpm;
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
    /// Callback delegate for retrieving the package id
    /// </summary>
    /// <typeparam name="T">Input item type</typeparam>
    /// <param name="item">Item tha can be mapped to a package id</param>
    /// <returns>Package id</returns>
    protected delegate string PackageIdCallback<T>(T item);

    /// <summary>
    /// Callback delegate for processing the package
    /// </summary>
    /// <typeparam name="T">Input item type</typeparam>
    /// <param name="package">WinGet package</param>
    /// <param name="item">Item that corresponds to the WinGet package</param>
    protected delegate Task PackageProcessorCallback<T>(IWinGetPackage package, T item);

    /// <summary>
    /// Get a list of packages from WinGet catalog ordered based on the input
    /// list and processes them according to the provided function
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <param name="items">List of objects that can be mapped to package IDs</param>
    /// <param name="packageIdCallback">Callback for retrieving the package id</param>
    /// <param name="packageProcessorCallback">Callback for processing the package</param>
    /// <returns>List of packages</returns>
    protected async Task<IList<IWinGetPackage>> GetOrderedPackagesAsync<T>(
        IList<T> items,
        PackageIdCallback<T> packageIdCallback,
        PackageProcessorCallback<T> packageProcessorCallback = null)
    {
        List<IWinGetPackage> result = new ();

        // Get packages from winget catalog
        var unorderedPackages = await _wpm.WinGetCatalog.GetPackagesAsync(items.Select(i => packageIdCallback(i)).ToHashSet());
        var unorderedPackagesMap = unorderedPackages.ToDictionary(p => p.Id, p => p);

        // Sort result based on the input
        foreach (var item in items)
        {
            var package = unorderedPackagesMap.GetValueOrDefault(packageIdCallback(item), null);
            if (package != null)
            {
                // Process package if a callback was provided
                if (packageProcessorCallback != null)
                {
                    await packageProcessorCallback(package, item);
                }

                result.Add(package);
            }
        }

        return result;
    }
}
