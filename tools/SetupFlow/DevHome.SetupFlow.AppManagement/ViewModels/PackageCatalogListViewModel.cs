// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly IHost _host;
    private readonly ILogger _logger;
    private readonly WinGetPackageJsonDataSource _jsonDataSource;
    private readonly WinGetPackageRestoreDataSource _restoreDataSource;
    private readonly string _packageCollectionsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "DevHome.SetupFlow", "Assets", "AppManagementPackages.json");

    /// <summary>
    /// Gets a list of package catalogs to display
    /// </summary>
    public ObservableCollection<PackageCatalogViewModel> PackageCatalogs { get; } = new ();

    /// <summary>
    /// Gets a list of shimmer indices.
    /// This list is used to repeat the shimmer control {Count} times
    /// </summary>
    public ObservableCollection<int> PackageCatalogShimmers { get; } = new ();

    /// <summary>
    /// Occurrs when a package catalog is loaded
    /// </summary>
    public event EventHandler<PackageCatalogViewModel> CatalogLoaded;

    public PackageCatalogListViewModel(IHost host, ILogger logger, WinGetPackageJsonDataSource jsonDataSource, WinGetPackageRestoreDataSource restoreDataSource)
    {
        _host = host;
        _logger = logger;
        _jsonDataSource = jsonDataSource;
        _restoreDataSource = restoreDataSource;
    }

    public async Task InitializeCatalogsAsync()
    {
        try
        {
            await _jsonDataSource.ImportCatalogsAsync(_packageCollectionsPath);
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Info, $"Exception thrown while initializing json data source");
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Local, e.Message);
        }
        finally
        {
            AddShimmers(_jsonDataSource.CatalogCount);
        }

        try
        {
            await _restoreDataSource.GetRestoreDeviceInfoAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Info, $"Exception thrown while initializing restore data source");
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Local, e.Message);
        }
        finally
        {
            AddShimmers(_restoreDataSource.CatalogCount);
        }
    }

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    public async Task LoadCatalogsAsync()
    {
        await LoadCatalogsFromDataSourceAsync(_restoreDataSource);
        await LoadCatalogsFromDataSourceAsync(_jsonDataSource);
    }

    private async Task LoadCatalogsFromDataSourceAsync(WinGetPackageDataSource dataSource)
    {
        if (dataSource.CatalogCount > 0)
        {
            // Load catalogs on a separate thread to avoid lagging the UI
            var catalogs = await Task.Run(async () => await dataSource.LoadCatalogsAsync());
            RemoveShimmers(dataSource.CatalogCount);
            foreach (var catalog in catalogs)
            {
                var catalogViewModel = _host.CreateInstance<PackageCatalogViewModel>(catalog);
                catalogViewModel.CanAddAllPackages = true;
                PackageCatalogs.Add(catalogViewModel);
                CatalogLoaded?.Invoke(null, catalogViewModel);
            }
        }
    }

    public void AddShimmers(int count)
    {
        while (count-- > 0)
        {
            PackageCatalogShimmers.Add(0);
        }
    }

    /// <summary>
    /// Removes a package catalog shimmer
    /// </summary>
    public void RemoveShimmers(int count)
    {
        while (count-- > 0 && PackageCatalogShimmers.Any())
        {
            PackageCatalogShimmers.Remove(PackageCatalogShimmers.Last());
        }
    }
}
