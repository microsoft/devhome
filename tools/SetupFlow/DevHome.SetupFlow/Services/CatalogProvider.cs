// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;
public class CatalogProvider : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new (initialCount: 1, maxCount: 1);
    private readonly IEnumerable<WinGetPackageDataSource> _dataSources = new List<WinGetPackageDataSource>();
    private List<PackageCatalog> _catalogs;

    public CatalogProvider(
        WinGetPackageJsonDataSource jsonDataSource,
        WinGetPackageRestoreDataSource restoreDataSource)
    {
        _dataSources = new List<WinGetPackageDataSource>()
        {
            restoreDataSource,
            jsonDataSource,
        };
    }

    public int CatalogCount => _dataSources.Sum(dataSource => dataSource.CatalogCount);

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var dataSource in _dataSources)
            {
                await dataSource.InitializeAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_catalogs != null)
            {
                return _catalogs;
            }

            _catalogs = new List<PackageCatalog>();
            foreach (var dataSource in _dataSources)
            {
                _catalogs.AddRange(await dataSource.LoadCatalogsAsync());
            }

            return _catalogs;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Clear()
    {
        _catalogs = null;
    }
}
