// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
public partial class PackageCatalogListViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly WinGetPackageJsonDataSource _jsonDataSource;
    private readonly string _packageCollectionsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "DevHome.SetupFlow", "Assets", "AppManagementPackages.json");

    /// <summary>
    /// Gets a list of package catalogs to display
    /// </summary>
    public ObservableCollection<PackageCatalogViewModel> PackageCatalogs { get; } = new ();

    /// <summary>
    /// Occurrs when a package catalog is loaded
    /// </summary>
    public event EventHandler<PackageCatalogViewModel> CatalogLoaded;

    public PackageCatalogListViewModel(ILogger logger, WinGetPackageJsonDataSource jsonDataSource)
    {
        _logger = logger;
        _jsonDataSource = jsonDataSource;
    }

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    public async Task LoadCatalogsAsync()
    {
        var allCatalogs = new List<PackageCatalog>();

        try
        {
            // Load catalogs from JSON file
            var catalogsFromJsonDataSource = await _jsonDataSource.LoadCatalogsAsync(_packageCollectionsPath);
            allCatalogs.AddRange(catalogsFromJsonDataSource);
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Info, $"Exception thrown while loading catalogs from json data source");
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Local, e.Message);
        }

        // TODO Load restore packages
        foreach (var catalog in allCatalogs)
        {
            var viewModel = new PackageCatalogViewModel(catalog);
            PackageCatalogs.Add(viewModel);
            CatalogLoaded?.Invoke(null, viewModel);
        }
    }
}
