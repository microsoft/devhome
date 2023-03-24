// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly IHost _host;
    private readonly ILogger _logger;
    private readonly PackageProvider _packageProvider;
    private readonly IWindowsPackageManager _wpm;
    private readonly WinGetPackageJsonDataSource _jsonDataSource;
    private readonly WinGetPackageRestoreDataSource _restoreDataSource;
    private bool _initialized;

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

    public PackageCatalogListViewModel(
        IHost host,
        ILogger logger,
        WinGetPackageJsonDataSource jsonDataSource,
        WinGetPackageRestoreDataSource restoreDataSource,
        IWindowsPackageManager wpm,
        PackageProvider packageProvider)
    {
        _host = host;
        _logger = logger;
        _jsonDataSource = jsonDataSource;
        _packageProvider = packageProvider;
        _restoreDataSource = restoreDataSource;
        _wpm = wpm;
    }

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    public async Task LoadCatalogsAsync()
    {
        if (!_initialized)
        {
            _initialized = true;

            // Initialize all data sources and load package ids into memory
            await InitializeDataSourceAsync(_jsonDataSource);
            await InitializeDataSourceAsync(_restoreDataSource);

            // Connect to winget catalog on a separate (non-UI) thread to prevent lagging the UI.
            await Task.Run(async () => await _wpm.WinGetCatalog.ConnectAsync());

            // Resolve package ids and create corresponding catalogs
            await LoadCatalogsFromDataSourceAsync(_restoreDataSource);
            await LoadCatalogsFromDataSourceAsync(_jsonDataSource);
        }
    }

    /// <summary>
    /// Initialize JSON data source
    /// </summary>
    private async Task InitializeDataSourceAsync(WinGetPackageDataSource dataSource)
    {
        try
        {
            await dataSource.InitializeAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Info, $"Exception thrown while initializing data source of type {dataSource.GetType().Name}");
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Local, e.Message);
        }
        finally
        {
            AddShimmers(dataSource.CatalogCount);
        }
    }

    /// <summary>
    /// Load catalogs from the provided data source and remove corresponding shimmers
    /// </summary>
    /// <param name="dataSource">Target data source</param>
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

    /// <summary>
    /// Add package catalog shimmers
    /// </summary>
    /// <param name="count">Number of package catalog shimmers to add</param>
    private void AddShimmers(int count)
    {
        while (count-- > 0)
        {
            PackageCatalogShimmers.Add(PackageCatalogShimmers.Count);
        }
    }

    /// <summary>
    /// Remove package catalog shimmers
    /// </summary>
    /// <param name="count">Number of package catalog shimmers to remove</param>
    private void RemoveShimmers(int count)
    {
        while (count-- > 0 && PackageCatalogShimmers.Any())
        {
            PackageCatalogShimmers.Remove(PackageCatalogShimmers.Last());
        }
    }
}
