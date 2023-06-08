// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Model class for a composite catalog from remote and/or local packages
/// </summary>
public class WinGetCompositeCatalog : IWinGetCatalog, IDisposable
{
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly CreateCompositePackageCatalogOptions _compositeCatalogOptions;
    private readonly SemaphoreSlim _connectionLock = new (1, 1);
    private Microsoft.Management.Deployment.PackageCatalog _catalog;
    private bool _disposedValue;

    public bool IsConnected => _catalog != null;

    public WinGetCompositeCatalog(WindowsPackageManagerFactory wingetFactory)
    {
        _wingetFactory = wingetFactory;
        _compositeCatalogOptions = _wingetFactory.CreateCreateCompositePackageCatalogOptions();
    }

    public void AddPackageCatalog(PackageCatalogReference catalog)
    {
        _compositeCatalogOptions.Catalogs.Add(catalog);
    }

    public CompositeSearchBehavior CompositeSearchBehavior
    {
        get => _compositeCatalogOptions.CompositeSearchBehavior;
        set => _compositeCatalogOptions.CompositeSearchBehavior = value;
    }

    public async Task ConnectAsync(bool forceReconnect)
    {
        await _connectionLock.WaitAsync();

        try
        {
            // Skip if already connected and should not force re-connect
            if (IsConnected && !forceReconnect)
            {
                return;
            }

            var packageManager = _wingetFactory.CreatePackageManager();
            var compositeCatalog = packageManager.CreateCompositePackageCatalog(_compositeCatalogOptions);
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Connecting to composite catalog");
            var connection = await compositeCatalog.ConnectAsync();
            if (connection.Status != ConnectResultStatus.Ok)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to catalog with status {connection.Status}");
                throw new CatalogConnectionException(connection.Status);
            }

            _catalog = connection.PackageCatalog;
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Error connecting to catalog reference.", e);
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit)
    {
        try
        {
            // Use default filter criteria for searching
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Searching for '{query}' on catalog {_catalog.Info.Name}. Result limit: {limit}");
            var options = _wingetFactory.CreateFindPackagesOptions();
            var filter = _wingetFactory.CreatePackageMatchFilter();
            filter.Field = PackageMatchField.CatalogDefault;
            filter.Option = PackageFieldMatchOption.ContainsCaseInsensitive;
            filter.Value = query;
            options.Selectors.Add(filter);
            options.ResultLimit = limit;

            return await FindPackagesAsync(options);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Error searching for packages.", e);
            throw;
        }
    }

    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<string> packageIdSet)
    {
        try
        {
            // Skip search if set is empty
            if (!packageIdSet.Any())
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"{nameof(GetPackagesAsync)} received an empty set of package id. Skipping search.");
                return new List<IWinGetPackage>();
            }

            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting package set from catalog {_catalog.Info.Name}");
            var options = _wingetFactory.CreateFindPackagesOptions();
            foreach (var packageId in packageIdSet)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Adding package [{packageId}] to query");
                var filter = _wingetFactory.CreatePackageMatchFilter();
                filter.Field = PackageMatchField.Id;
                filter.Option = PackageFieldMatchOption.Equals;
                filter.Value = packageId;
                options.Selectors.Add(filter);
            }

            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Starting search for packages");
            return await FindPackagesAsync(options);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Error getting packages.", e);
            throw;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _connectionLock.Dispose();
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
    /// Core method for finding packages based on the provided options
    /// </summary>
    /// <param name="options">Find packages options</param>
    /// <returns>List of winget package matches</returns>
    /// <exception cref="InvalidOperationException">Exception thrown if the catalog is not connected before attempting to find packages</exception>
    /// <exception cref="FindPackagesException">Exception thrown if the find packages operation failed</exception>
    private async Task<IList<IWinGetPackage>> FindPackagesAsync(FindPackagesOptions options)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException($"Cannot perform {FindPackagesAsync} operation because the catalog reference is not connected");
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Performing search");
        var result = new List<IWinGetPackage>();
        var findResult = await _catalog.FindPackagesAsync(options);
        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to find packages with status {findResult.Status}");
            throw new FindPackagesException(findResult.Status);
        }

        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Found {findResult.Matches} results");

        // Cannot use foreach or LINQ for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        for (var i = 0; i < findResult.Matches.Count; ++i)
        {
            var catalogPackage = findResult.Matches[i].CatalogPackage;
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Found [{catalogPackage.Id}]");
            result.Add(new WinGetPackage(catalogPackage));
        }

        return result;
    }
}
