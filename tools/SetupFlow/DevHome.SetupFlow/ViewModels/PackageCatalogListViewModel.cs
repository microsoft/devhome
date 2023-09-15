// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.ViewModels;

public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly IWindowsPackageManager _wpm;
    private readonly CatalogDataSourceLoacder _catalogDataSourceLoacder;
    private readonly PackageCatalogViewModelFactory _packageCatalogViewModelFactory;
    private bool _initialized;

    /// <summary>
    /// Gets a list of package catalogs to display
    /// </summary>
    public ObservableCollection<PackageCatalogViewModel> PackageCatalogs { get; } = new();

    /// <summary>
    /// Gets a list of shimmer indices.
    /// This list is used to repeat the shimmer control {Count} times
    /// </summary>
    public ObservableCollection<int> PackageCatalogShimmers { get; } = new();

    public PackageCatalogListViewModel(
        CatalogDataSourceLoacder catalogDataSourceLoacder,
        IWindowsPackageManager wpm,
        PackageCatalogViewModelFactory packageCatalogViewModelFactory)
    {
        _catalogDataSourceLoacder = catalogDataSourceLoacder;
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
            AddShimmers(_catalogDataSourceLoacder.CatalogCount);
            try
            {
                await Task.Run(async () => await _wpm.WinGetCatalog.ConnectAsync());
                await foreach (var dataSourceCatalogs in _catalogDataSourceLoacder.LoadCatalogsAsync())
                {
                    foreach (var catalog in dataSourceCatalogs)
                    {
                        var catalogVM = await Task.Run(() => _packageCatalogViewModelFactory(catalog));
                        catalogVM.CanAddAllPackages = true;
                        PackageCatalogs.Add(catalogVM);
                    }

                    RemoveShimmers(dataSourceCatalogs.Count);
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to {nameof(_wpm.WinGetCatalog)}. Skipping catalogs loading operation.", e);
            }

            // Remove any remaining shimmers:
            // This can happen if for example a catalog was detected but not
            // displayed (e.g. catalog with no packages to display)
            RemoveShimmers(_catalogDataSourceLoacder.CatalogCount);
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
