// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
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
    /// Get a list of packages from WinGet catalog ordered based on the input
    /// list and processes them according to the provided function
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <param name="items">List of objects that can be mapped to package IDs</param>
    /// <param name="packageIdFunc">Function for retrieving the package id</param>
    /// <param name="packageProcessorFunc">Function for processing the package</param>
    /// <returns>List of packages</returns>
    protected async Task<IList<IWinGetPackage>> GetOrderedPackagesAsync<T>(
        IList<T> items,
        Func<T, string> packageIdFunc,
        Func<IWinGetPackage, T, Task> packageProcessorFunc = null)
    {
        List<IWinGetPackage> result = new ();

        // Get packages from winget catalog
        var unorderedPackages = await _wpm.WinGetCatalog.GetPackagesAsync(items.Select(packageIdFunc).ToHashSet());
        var unorderedPackagesMap = unorderedPackages.ToDictionary(p => p.Id, p => p);

        // Sort result based on the input
        foreach (var item in items)
        {
            var package = unorderedPackagesMap.GetValueOrDefault(packageIdFunc(item), null);
            if (package != null)
            {
                // Process package if a function was provided
                if (packageProcessorFunc != null)
                {
                    await packageProcessorFunc(package, item);
                }

                result.Add(package);
            }
        }

        return result;
    }
}
