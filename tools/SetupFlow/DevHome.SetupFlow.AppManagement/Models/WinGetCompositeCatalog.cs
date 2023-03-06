// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Exceptions;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.Telemetry;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Models;

/// <summary>
/// Model class for a composite catalog from remote and/or local packages
/// </summary>
public class WinGetCompositeCatalog : IWinGetCatalog
{
    private readonly ILogger _logger;
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly CreateCompositePackageCatalogOptions _compositeCatalogOptions;
    private Microsoft.Management.Deployment.PackageCatalog _catalog;

    public bool IsConnected => _catalog != null;

    public WinGetCompositeCatalog(ILogger logger, WindowsPackageManagerFactory wingetFactory)
    {
        _logger = logger;
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

    public async Task ConnectAsync()
    {
        // Skip if already connected
        if (IsConnected)
        {
            return;
        }

        try
        {
            var packageManager = _wingetFactory.CreatePackageManager();
            var compositeCatalog = packageManager.CreateCompositePackageCatalog(_compositeCatalogOptions);
            var connection = await compositeCatalog.ConnectAsync();
            if (connection.Status != ConnectResultStatus.Ok)
            {
                _logger.LogError(nameof(CatalogConnectionException), LogLevel.Info, $"Failed to connect to catalog with status {connection.Status}");
                throw new CatalogConnectionException(connection.Status);
            }

            _catalog = connection.PackageCatalog;
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetCompositeCatalog), LogLevel.Info, $"Error connecting to catalog reference: {e.Message}");
            throw;
        }
    }

    public async Task<IList<IWinGetPackage>> SearchAsync(string query)
    {
        try
        {
            // Use default filter criteria for searching
            var options = _wingetFactory.CreateFindPackagesOptions();
            var filter = _wingetFactory.CreatePackageMatchFilter();
            filter.Field = PackageMatchField.CatalogDefault;
            filter.Option = PackageFieldMatchOption.ContainsCaseInsensitive;
            filter.Value = query;
            options.Selectors.Add(filter);

            return await FindPackagesAsync(options);
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetCompositeCatalog), LogLevel.Info, $"Error searching for packages: {e.Message}");
            throw;
        }
    }

    public async Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<string> packageIdSet)
    {
        try
        {
            var options = _wingetFactory.CreateFindPackagesOptions();
            foreach (var packageId in packageIdSet)
            {
                var filter = _wingetFactory.CreatePackageMatchFilter();
                filter.Field = PackageMatchField.Id;
                filter.Option = PackageFieldMatchOption.Equals;
                filter.Value = packageId;
                options.Selectors.Add(filter);
            }

            return await FindPackagesAsync(options);
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetCompositeCatalog), LogLevel.Info, $"Error searching for packages: {e.Message}");
            throw;
        }
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

        var result = new List<IWinGetPackage>();
        var findResult = await _catalog.FindPackagesAsync(options);
        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            _logger.LogError(nameof(FindPackagesException), LogLevel.Info, $"Failed to find packages with status {findResult.Status}");
            throw new FindPackagesException(findResult.Status);
        }

        // Cannot use foreach or Linq for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        for (var i = 0; i < findResult.Matches.Count; ++i)
        {
            var catalogPackage = findResult.Matches[i].CatalogPackage;
            result.Add(new WinGetPackage(catalogPackage));
        }

        return result;
    }
}
