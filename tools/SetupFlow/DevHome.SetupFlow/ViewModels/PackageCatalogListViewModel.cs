// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Dispatching;

namespace DevHome.SetupFlow.ViewModels;
public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly IWindowsPackageManager _wpm;
    private readonly CatalogProvider _catalogProvider;
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
        CatalogProvider catalogProvider,
        IWindowsPackageManager wpm,
        PackageCatalogViewModelFactory packageCatalogViewModelFactory)
    {
        _catalogProvider = catalogProvider;
        _wpm = wpm;
        _packageCatalogViewModelFactory = packageCatalogViewModelFactory;
    }

#pragma warning disable

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    public async Task LoadCatalogsAsync()
    {
        if (!_initialized)
        {
            _initialized = true;
            AddShimmers(_catalogProvider.CatalogCount);
            await Task.Run(async () => await _wpm.WinGetCatalog.ConnectAsync());
            await foreach (var dataSourceCatalogs in _catalogProvider.LoadCatalogsAsync())
            {
                foreach (var catalog in dataSourceCatalogs)
                {
                    var catalogVM = await Task.Run(() => _packageCatalogViewModelFactory(catalog));
                    catalogVM.CanAddAllPackages = true;
                    PackageCatalogs.Add(catalogVM);
                }

                RemoveShimmers(dataSourceCatalogs.Count);
            }

            // Remove any remaining shimmers
            RemoveShimmers(_catalogProvider.CatalogCount);
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
