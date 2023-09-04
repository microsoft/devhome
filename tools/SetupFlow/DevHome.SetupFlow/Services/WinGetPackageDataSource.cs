// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;

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
    /// <param name="item">Item that can be mapped to a package id</param>
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
    /// Get a list of packages from WinGet catalog ordered based
    /// on the input list and processes them according to the provided callback
    /// function
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <param name="items">List of objects that can be mapped to package IDs</param>
    /// <param name="packageIdCallback">Callback for retrieving the package id</param>
    /// <param name="packageProcessorCallback">Callback for processing a package</param>
    /// <returns>List of packages</returns>
    protected async Task<IList<IWinGetPackage>> GetPackagesAsync<T>(
        IList<T> items,
        PackageIdCallback<T> packageIdCallback,
        PackageProcessorCallback<T> packageProcessorCallback = null)
    {
        List<IWinGetPackage> result = new ();

        // Skip search if package data source is empty
        if (!items.Any())
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"{nameof(GetPackagesAsync)} received an empty list of items. Skipping search.");
            return result;
        }

        // Get packages from winget catalog
        var unorderedPackages = await _wpm.WinGetCatalog.GetPackagesAsync(items.Select(i => packageIdCallback(i)).ToHashSet());
        var unorderedPackagesMap = new Dictionary<string, IWinGetPackage>();
        foreach (var package in unorderedPackages)
        {
            try
            {
                var packageUri = _wpm.CreatePackageUri(package);
                unorderedPackagesMap.Add(packageUri.ToString(), package);
            }
            catch
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Failed to create package uri for [{package.Id}]");
            }
        }

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
