// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Behaviors;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.ViewModels;

public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly ICatalogDataSourceLoader _catalogDataSourceLoader;
    private readonly PackageCatalogViewModelFactory _packageCatalogViewModelFactory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CatalogFullPath))]
    private PackageCatalogViewModel _viewAllCatalog;

    public List<string> CatalogFullPath => new()
    {
        AppManagementBehavior.Title,
        ViewAllCatalog?.Name ?? string.Empty,
    };

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
        ICatalogDataSourceLoader catalogDataSourceLoader,
        PackageCatalogViewModelFactory packageCatalogViewModelFactory)
    {
        _catalogDataSourceLoader = catalogDataSourceLoader;
        _packageCatalogViewModelFactory = packageCatalogViewModelFactory;
    }

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    public async Task LoadCatalogsAsync()
    {
        AddShimmers(_catalogDataSourceLoader.CatalogCount);
        try
        {
            await foreach (var dataSourceCatalogs in _catalogDataSourceLoader.LoadCatalogsAsync())
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
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to load catalogs.", e);
        }

        // Remove any remaining shimmers:
        // This can happen if for example a catalog was detected but not
        // displayed (e.g. catalog with no packages to display)
        RemoveShimmers(_catalogDataSourceLoader.CatalogCount);
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

    [RelayCommand]
    private void ViewAllPackages(PackageCatalogViewModel catalog)
    {
        AppManagementBehavior.SetHeaderVisibility(false);
        ViewAllCatalog = catalog;
    }

    [RelayCommand]
    private void ExitViewAllPackages()
    {
        AppManagementBehavior.SetHeaderVisibility(true);
        ViewAllCatalog = null;
    }

    [RelayCommand]
    private void OnLoaded()
    {
        // When the view is loaded, ensure we exit the view all packages mode
        ExitViewAllPackages();
    }
}
