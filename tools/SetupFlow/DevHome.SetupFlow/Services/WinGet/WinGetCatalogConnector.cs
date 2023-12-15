// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Extensions;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Services.WinGet;

public class WinGetCatalogConnector : IWinGetCatalogConnector, IDisposable
{
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly Dictionary<string, WinGetCatalog> _customCatalogs = new ();
    private readonly SemaphoreSlim _lock = new (1, 1);

    // Predefined and custom catalogs
    private WinGetCatalog _predefinedWingetCatalog;
    private WinGetCatalog _predefinedMsStoreCatalog;
    private WinGetCatalog _customSearchCatalog;

    // Predefined catalogs ids
    private string _predefinedWingetCatalogId;
    private string _predefinedMsStoreCatalogId;

    private bool _disposedValue;

    public WinGetCatalogConnector(WindowsPackageManagerFactory wingetFactory)
    {
        _wingetFactory = wingetFactory;
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> GetPredefinedWingetCatalogAsync()
    {
        await _lock.WaitAsync();

        try
        {
            return _predefinedWingetCatalog;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> GetPredefinedMsStoreCatalogAsync()
    {
        await _lock.WaitAsync();

        try
        {
            return _predefinedMsStoreCatalog;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> GetCustomSearchCatalogAsync()
    {
        await _lock.WaitAsync();

        try
        {
            return _customSearchCatalog;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> GetPackageCatalogAsync(IWinGetPackage package)
    {
        // 'winget' catalog
        if (IsWinGetPackage(package))
        {
            return await GetPredefinedWingetCatalogAsync();
        }

        // 'msstore' catalog
        if (IsMsStorePackage(package))
        {
            return await GetPredefinedMsStoreCatalogAsync();
        }

        // custom catalog
        return await GetPackageCatalogByNameAsync(package.CatalogName);
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> GetPackageCatalogByNameAsync(string catalogName)
    {
        // Get custom catalog from cache or connect to it then cache it
        await _lock.WaitAsync();
        try
        {
            if (_customCatalogs.TryGetValue(catalogName, out var catalog))
            {
                return catalog;
            }

            var packageManager = _wingetFactory.CreatePackageManager();
            var customCatalog = packageManager.GetPackageCatalogByName(catalogName);
            var result = await customCatalog.ConnectAsync();
            if (result.Status != ConnectResultStatus.Ok)
            {
                throw new InvalidOperationException($"Failed to connect to catalog {catalogName} with status {result.Status}");
            }

            var resultCatalog = new WinGetCatalog(result.PackageCatalog, WinGetCatalog.CatalogType.CustomUnknown, catalogName);
            _customCatalogs[catalogName] = resultCatalog;
            return resultCatalog;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public bool IsMsStorePackage(IWinGetPackage package) => _predefinedMsStoreCatalogId == null ? false : package.CatalogId == _predefinedMsStoreCatalogId;

    /// <inheritdoc/>
    public bool IsWinGetPackage(IWinGetPackage package) => _predefinedWingetCatalogId == null ? false : package.CatalogId == _predefinedWingetCatalogId;

    /// <inheritdoc/>
    public async Task CreateAndConnectCatalogsAsync()
    {
        // Extract catalog ids for predefined catalogs
        _predefinedWingetCatalogId ??= GetPredefinedCatalogId(PredefinedPackageCatalog.OpenWindowsCatalog);
        _predefinedMsStoreCatalogId ??= GetPredefinedCatalogId(PredefinedPackageCatalog.MicrosoftStore);

        // Create and connect to predefined catalogs concurrently
        var searchCatalog = CreateAndConnectSearchCatalogAsync();
        var wingetCatalog = CreateAndConnectWinGetCatalogAsync();
        var msStoreCatalog = CreateAndConnectMsStoreCatalogAsync();
        await Task.WhenAll(searchCatalog, wingetCatalog, msStoreCatalog);
        _customSearchCatalog = searchCatalog.Result;
        _predefinedWingetCatalog = wingetCatalog.Result;
        _predefinedMsStoreCatalog = msStoreCatalog.Result;

        // Clear custom catalogs
        _customCatalogs.Clear();
    }

    /// <summary>
    /// Create and connect to the search catalog consisting of all the package catalogs.
    /// </summary>
    /// <returns>Search catalog or null if an error occurred</returns>
    private async Task<WinGetCatalog> CreateAndConnectSearchCatalogAsync()
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var catalogs = packageManager.GetPackageCatalogs();
            return new (await CreateAndConnectCatalogAsync(catalogs), WinGetCatalog.CatalogType.CustomSearch);
        }
        catch
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to create and/or connect to search catalog.");
            return null;
        }
    }

    /// <summary>
    /// Create and connect to the winget catalog
    /// </summary>
    /// <returns>Winget catalog or null if an error occurred</returns>
    private async Task<WinGetCatalog> CreateAndConnectWinGetCatalogAsync()
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var catalog = packageManager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
            return new (await CreateAndConnectCatalogAsync(new List<PackageCatalogReference>() { catalog }), WinGetCatalog.CatalogType.PredefinedWinget);
        }
        catch
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to create or connect to 'winget' catalog source.");
            return null;
        }
    }

    /// <summary>
    /// Create and connect to the MS store catalog
    /// </summary>
    /// <returns>MS store catalog or null if an error occurred</returns>
    private async Task<WinGetCatalog> CreateAndConnectMsStoreCatalogAsync()
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var catalog = packageManager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.MicrosoftStore);
            return new (await CreateAndConnectCatalogAsync(new List<PackageCatalogReference>() { catalog }), WinGetCatalog.CatalogType.PredefinedMsStore);
        }
        catch
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to create or connect to 'msstore' catalog source.");
            return null;
        }
    }

    private async Task<WPMPackageCatalog> CreateAndConnectCatalogAsync(IReadOnlyList<PackageCatalogReference> catalogReferences)
    {
        // Search in all catalogs including the local catalog which allows detecting if a package is installed
        var disconnectedCatalog = _wingetFactory.CreateCompositePackageCatalog(CompositeSearchBehavior.RemotePackagesFromAllCatalogs, catalogReferences);
        var connectResult = await disconnectedCatalog.ConnectAsync();
        if (connectResult.Status == ConnectResultStatus.Ok)
        {
            return connectResult.PackageCatalog;
        }

        Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to catalog with status {connectResult.Status}");
        return null;
    }

    /// <summary>
    /// Gets the id of the provided predefined catalog
    /// </summary>
    /// <param name="catalog">Predefined catalog</param>
    /// <returns>Catalog id</returns>
    private string GetPredefinedCatalogId(PredefinedPackageCatalog catalog)
    {
        var packageManager = _wingetFactory.CreatePackageManager();
        var packageCatalog = packageManager.GetPredefinedPackageCatalog(catalog);
        return packageCatalog.Info.Id;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _lock.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
