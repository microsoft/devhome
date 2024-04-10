// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Extensions;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;
using Serilog;
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Services.WinGet;

internal sealed class WinGetCatalogConnector : IWinGetCatalogConnector, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WinGetCatalogConnector));
    private readonly IWinGetPackageCache _packageCache;
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly Dictionary<string, WinGetCatalog> _customCatalogs = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private WinGetCatalog _predefinedWingetCatalog;
    private WinGetCatalog _predefinedMsStoreCatalog;
    private WinGetCatalog _customSearchCatalog;

    private string _predefinedWingetCatalogId;
    private string _predefinedMsStoreCatalogId;

    private bool _disposedValue;

    public WinGetCatalogConnector(
        WindowsPackageManagerFactory wingetFactory,
        IWinGetPackageCache packageCache)
    {
        _packageCache = packageCache;
        _wingetFactory = wingetFactory;
    }

    /// <inheritdoc/>
    public async Task<WinGetCatalog> GetPredefinedWingetCatalogAsync()
    {
        // Use SemaphoreSlim on catalog to:
        // 1. ensure reading the latest written value
        // 2. block other threads from reading the catalog while it's being written
        // 3. ReaderWriterLockSlim is not used here to prevent threading issues
        //    such as entering and exiting locks from different threads after
        //    awaiting on a task.
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
        // Use SemaphoreSlim on catalog to:
        // 1. ensure reading the latest written value
        // 2. block other threads from reading the catalog while it's being written
        // 3. ReaderWriterLockSlim is not used here to prevent threading issues
        //    such as entering and exiting locks from different threads after
        //    awaiting on a task.
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
        // Use SemaphoreSlim on catalog to:
        // 1. ensure reading the latest written value
        // 2. block other threads from reading the catalog while it's being written
        // 3. ReaderWriterLockSlim is not used here to prevent threading issues
        //    such as entering and exiting locks from different threads after
        //    awaiting on a task.
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
    public async Task<WinGetCatalog> GetPackageCatalogByNameAsync(string catalogName)
    {
        // Use SemaphoreSlim on catalog to:
        // 1. ensure reading the latest written value
        // 2. block other threads from reading the catalog while it's being written
        // 3. ReaderWriterLockSlim is not used here to prevent threading issues
        //    such as entering and exiting locks from different threads after
        //    awaiting on a task.
        await _lock.WaitAsync();
        try
        {
            if (!_customCatalogs.TryGetValue(catalogName, out var catalog))
            {
                catalog = await CreateAndConnectCustomCatalogAsync(catalogName);
                if (catalog != null)
                {
                    _customCatalogs[catalogName] = catalog;
                }
            }

            return catalog;
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
    public async Task CreateAndConnectCatalogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            ClearAllCaches();

            // Create and connect to predefined catalogs concurrently
            await Task.WhenAll(
                CreateAndConnectSearchCatalogAsync(),
                CreateAndConnectWinGetCatalogAsync(),
                CreateAndConnectMsStoreCatalogAsync());
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RecoverDisconnectedCatalogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            ClearAllCaches();

            // Recover catalogs that are not alive concurrently
            var recoverSearchCatalog = !IsCatalogAlive(_customSearchCatalog);
            var recoverWinGetCatalog = !IsCatalogAlive(_predefinedWingetCatalog);
            var recoverMsStoreCatalog = !IsCatalogAlive(_predefinedMsStoreCatalog);
            _log.Information($"Recovering disconnected catalogs [should recover?]: Search [{recoverSearchCatalog}], WinGet [{recoverWinGetCatalog}], MsStore [{recoverMsStoreCatalog}]");
            await Task.WhenAll(
                recoverSearchCatalog ? CreateAndConnectSearchCatalogAsync() : Task.CompletedTask,
                recoverWinGetCatalog ? CreateAndConnectWinGetCatalogAsync() : Task.CompletedTask,
                recoverMsStoreCatalog ? CreateAndConnectMsStoreCatalogAsync() : Task.CompletedTask);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public bool IsMsStorePackage(IWinGetPackage package) => package.CatalogId == _predefinedMsStoreCatalogId;

    /// <inheritdoc/>
    public bool IsWinGetPackage(IWinGetPackage package) => package.CatalogId == _predefinedWingetCatalogId;

    /// <summary>
    /// Clear all caches
    /// </summary>
    private void ClearAllCaches()
    {
        // Clear package cache so next time they are fetched from the new
        // catalog connections
        _packageCache.Clear();

        // Clear custom catalogs so they are re-created and connected on demand
        _customCatalogs.Clear();
    }

    /// <summary>
    /// Create and connect to the search catalog consisting of all the package catalogs.
    /// </summary>
    private async Task CreateAndConnectSearchCatalogAsync()
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var catalogs = packageManager.GetPackageCatalogs();
            _customSearchCatalog = new(await CreateAndConnectCatalogInternalAsync(catalogs), WinGetCatalog.CatalogType.CustomSearch);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to create or connect to search catalog.");
        }
    }

    /// <summary>
    /// Create and connect to the winget catalog
    /// </summary>
    private async Task CreateAndConnectWinGetCatalogAsync()
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var catalog = packageManager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
            _predefinedWingetCatalogId ??= catalog.Info.Id;
            _predefinedWingetCatalog = new(await CreateAndConnectCatalogInternalAsync(new List<PackageCatalogReference>() { catalog }), WinGetCatalog.CatalogType.PredefinedWinget);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to create or connect to 'winget' catalog source.");
        }
    }

    /// <summary>
    /// Create and connect to the MS store catalog
    /// </summary>
    private async Task CreateAndConnectMsStoreCatalogAsync()
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var catalog = packageManager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.MicrosoftStore);
            _predefinedMsStoreCatalogId ??= catalog.Info.Id;
            _predefinedMsStoreCatalog = new(await CreateAndConnectCatalogInternalAsync(new List<PackageCatalogReference>() { catalog }), WinGetCatalog.CatalogType.PredefinedMsStore);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to create or connect to 'msstore' catalog source.");
        }
    }

    /// <summary>
    /// Create and connect to a custom catalog
    /// </summary>
    /// <param name="catalogName">Catalog name</param>
    /// <returns>Custom catalog or null if an error occurred</returns>
    private async Task<WinGetCatalog> CreateAndConnectCustomCatalogAsync(string catalogName)
    {
        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var customCatalog = packageManager.GetPackageCatalogByName(catalogName);
            return new(await CreateAndConnectCatalogInternalAsync(new List<PackageCatalogReference>() { customCatalog }), WinGetCatalog.CatalogType.CustomUnknown, catalogName);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to create or connect to custom catalog with name {catalogName}");
            return null;
        }
    }

    /// <summary>
    /// Core method for creating and connecting to a catalog
    /// </summary>
    /// <param name="catalogReferences">Catalog references</param>
    /// <returns>Connected catalog or null if an error occurred</returns>
    private async Task<WPMPackageCatalog> CreateAndConnectCatalogInternalAsync(IReadOnlyList<PackageCatalogReference> catalogReferences)
    {
        // Search in all catalogs including the local catalog which allows detecting if a package is installed
        var disconnectedCatalog = _wingetFactory.CreateCompositePackageCatalog(CompositeSearchBehavior.RemotePackagesFromAllCatalogs, catalogReferences);
        var connectResult = await disconnectedCatalog.ConnectAsync();
        if (connectResult.Status == ConnectResultStatus.Ok)
        {
            return connectResult.PackageCatalog;
        }

        _log.Error($"Failed to connect to catalog with status {connectResult.Status}");
        return null;
    }

    /// <summary>
    /// Check if the provided catalog is alive
    /// </summary>
    /// <param name="catalog">Target catalog</param>
    /// <returns>True if the catalog is alive</returns>
    private bool IsCatalogAlive(WinGetCatalog catalog)
    {
        try
        {
            // Attempt to access the catalog name to verify that the catalog's out-of-proc object is still alive
            catalog?.Catalog.Info.Name.ToString();
            return catalog != null;
        }
        catch
        {
            return false;
        }
    }

    private void Dispose(bool disposing)
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
