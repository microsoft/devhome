// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;

public class CatalogDataSourceLoader : ICatalogDataSourceLoader, IDisposable
{
    private readonly SemaphoreSlim _lock = new(initialCount: 1, maxCount: 1);
    private readonly IEnumerable<WinGetPackageDataSource> _dataSources;
    private bool _disposedValue;

    public CatalogDataSourceLoader(IEnumerable<WinGetPackageDataSource> dataSources)
    {
        _dataSources = dataSources;
    }

    /// <inheritdoc />
    public int CatalogCount => _dataSources.Sum(dataSource => dataSource.CatalogCount);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async IAsyncEnumerable<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var dataSource in _dataSources)
            {
                yield return await LoadCatalogsFromDataSourceAsync(dataSource) ?? new List<PackageCatalog>();
            }
        }
        finally
        {
            _lock.Release();
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

    public void Clear()
    {
        _catalogsMap.Clear();
    }

            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Loading winget packages from data source {dataSource.GetType().Name}");
            return await Task.Run(async () => await dataSource.LoadCatalogsAsync());
            {
                _lock.Dispose();
            }

            _disposedValue = true;
        }
        return null;
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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Clear()
    {
        _catalogsMap.Clear();
    }

            if (dataSource.CatalogCount > 0)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Loading winget packages from data source {dataSource.GetType().Name}");
                var catalogs = await Task.Run(async () => await dataSource.LoadCatalogsAsync());
                dataSourceCatalogs.AddRange(catalogs);
            }
        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initializing package list from data source {dataSource.GetType().Name}");
            await dataSource.InitializeAsync();
        }
        catch (Exception e)
        return dataSourceCatalogs;
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Exception thrown while initializing data source of type {dataSource.GetType().Name}", e);
        }
    }

    /// <summary>
    /// Load catalogs from the provided data source
    /// </summary>
    /// <param name="dataSource">Target data source</param>
    private async Task<IList<PackageCatalog>> LoadCatalogsFromDataSourceAsync(WinGetPackageDataSource dataSource)
    {
        try
        {
            if (dataSource.CatalogCount > 0)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Loading winget packages from data source {dataSource.GetType().Name}");
                var catalogs = await Task.Run(async () => await dataSource.LoadCatalogsAsync());
                dataSourceCatalogs.AddRange(catalogs);
            }
            }
            }
            }
            }
            }
                _lock.Dispose();
        return dataSourceCatalogs;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
