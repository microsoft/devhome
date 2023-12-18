// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;
public class CatalogDataSourceLoader : IDisposable
{
    private readonly SemaphoreSlim _lock = new (initialCount: 1, maxCount: 1);
    private readonly IEnumerable<WinGetPackageDataSource> _dataSources;
    private readonly Dictionary<WinGetPackageDataSource, IList<PackageCatalog>> _catalogsMap = new ();
    private bool _disposedValue;

    public CatalogDataSourceLoader(IEnumerable<WinGetPackageDataSource> dataSources)
    {
        _dataSources = dataSources;
    }

    public int CatalogCount => _dataSources.Sum(dataSource => dataSource.CatalogCount);

    /// <summary>
    /// Initialize all data sources
    /// </summary>
    public async Task InitializeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var dataSource in _dataSources)
            {
                await InitializeDataSourceAsync(dataSource);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Load catalogs from all data sources
    /// </summary>
    /// <returns>Catalogs from all data sources</returns>
    public async IAsyncEnumerable<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await foreach (var catalog in LoadCatalogsInternalAsync(clearCache: false))
            {
                yield return catalog;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Reload catalogs from all data sources
    /// </summary>
    /// <returns>Catalogs from all data sources</returns>
    public async IAsyncEnumerable<IList<PackageCatalog>> ReloadCatalogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await foreach (var catalog in LoadCatalogsInternalAsync(clearCache: true))
            {
                yield return catalog;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Core logic to load catalogs from all data sources
    /// </summary>
    /// <returns>Catalogs from all data sources</returns>
    private async IAsyncEnumerable<IList<PackageCatalog>> LoadCatalogsInternalAsync(bool clearCache)
    {
        if (clearCache)
        {
            _catalogsMap.Clear();
        }

        foreach (var dataSource in _dataSources)
        {
            IList<PackageCatalog> dataSourceCatalogs;
            if (!_catalogsMap.TryGetValue(dataSource, out dataSourceCatalogs))
            {
                dataSourceCatalogs = await LoadCatalogsFromDataSourceAsync(dataSource);
                _catalogsMap.TryAdd(dataSource, dataSourceCatalogs);
            }

            yield return dataSourceCatalogs;
        }
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

    /// <summary>
    /// Initialize data source
    /// </summary>
    private async Task InitializeDataSourceAsync(WinGetPackageDataSource dataSource)
    {
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initializing package list from data source {dataSource.GetType().Name}");
            await dataSource.InitializeAsync();
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Exception thrown while initializing data source of type {dataSource.GetType().Name}", e);
        }
    }

    /// <summary>
    /// Load catalogs from the provided data source
    /// </summary>
    /// <param name="dataSource">Target data source</param>
    private async Task<IList<PackageCatalog>> LoadCatalogsFromDataSourceAsync(WinGetPackageDataSource dataSource)
    {
        var dataSourceCatalogs = new List<PackageCatalog>();
        try
        {
            if (dataSource.CatalogCount > 0)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Loading winget packages from data source {dataSource.GetType().Name}");
                var catalogs = await Task.Run(async () => await dataSource.LoadCatalogsAsync());
                dataSourceCatalogs.AddRange(catalogs);
            }
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Exception thrown while loading data source of type {dataSource.GetType().Name}", e);
        }

        return dataSourceCatalogs;
    }
}
