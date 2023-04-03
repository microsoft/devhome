// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly IWindowsPackageManager _wpm;
    private readonly WinGetPackageJsonDataSource _jsonDataSource;
    private readonly WinGetPackageRestoreDataSource _restoreDataSource;
    private readonly PackageCatalogViewModelFactory _packageCatalogViewModelFactory;
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

    public PackageCatalogListViewModel(
        WinGetPackageJsonDataSource jsonDataSource,
        WinGetPackageRestoreDataSource restoreDataSource,
        IWindowsPackageManager wpm,
        PackageCatalogViewModelFactory packageCatalogViewModelFactory)
    {
        _jsonDataSource = jsonDataSource;
        _restoreDataSource = restoreDataSource;
        _wpm = wpm;
        _packageCatalogViewModelFactory = packageCatalogViewModelFactory;
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
        catch (Exception)
        {
            //// _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Info, $"Exception thrown while initializing data source of type {dataSource.GetType().Name}");
            //// _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Local, e.Message);
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
                var catalogViewModel = _packageCatalogViewModelFactory(catalog);
                catalogViewModel.CanAddAllPackages = true;
                PackageCatalogs.Add(catalogViewModel);
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
