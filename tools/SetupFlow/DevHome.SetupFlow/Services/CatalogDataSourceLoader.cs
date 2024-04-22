// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using Serilog;

namespace DevHome.SetupFlow.Services;

public class CatalogDataSourceLoader : ICatalogDataSourceLoader, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CatalogDataSourceLoader));
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

    /// <summary>
    /// Initialize data source
    /// </summary>
    private async Task InitializeDataSourceAsync(WinGetPackageDataSource dataSource)
    {
        try
        {
            _log.Information($"Initializing package list from data source {dataSource.GetType().Name}");
            await dataSource.InitializeAsync();
        }
        catch (Exception e)
        {
            _log.Error(e, $"Exception thrown while initializing data source of type {dataSource.GetType().Name}");
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
            _log.Information($"Loading winget packages from data source {dataSource.GetType().Name}");
            return await Task.Run(async () => await dataSource.LoadCatalogsAsync());
        }
        catch (Exception e)
        {
            _log.Error(e, $"Exception thrown while loading data source of type {dataSource.GetType().Name}");
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
    }
}
