// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    private readonly WinGetPackageJsonDataSource _jsonDataSource;
    private readonly string _packageCollectionsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "DevHome.SetupFlow", "Assets", "AppManagementPackages.json");

    /// <summary>
    /// Gets a list of package catalogs to display
    /// </summary>
    public ObservableCollection<PackageCatalogViewModel> PackageCatalogs { get; } = new ();

    /// <summary>
    /// Gets a list of shimmer indices.
    /// This list is used to repeat the shimmer control {Count} times
    /// </summary>
    public ObservableCollection<int> PackageCatalogShimmers { get; } = new (Enumerable.Range(0, 1));

    /// <summary>
    /// Occurrs when a package catalog is loaded
    /// </summary>
    public event EventHandler<PackageCatalogViewModel> CatalogLoaded;

    public PackageCatalogListViewModel(IHost host, ILogger logger, WinGetPackageJsonDataSource jsonDataSource)
    {
        _host = host;
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
            // Load catalogs from JSON file and resolve package ids from winget
            // on a separate thread to avoid lagging the UI
            var catalogsFromJsonDataSource = await Task.Run(async () => await _jsonDataSource.LoadCatalogsAsync(_packageCollectionsPath));
            allCatalogs.AddRange(catalogsFromJsonDataSource);
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Info, $"Exception thrown while loading catalogs from json data source");
            _logger.LogError(nameof(PackageCatalogListViewModel), LogLevel.Local, e.Message);
        }
        finally
        {
            RemoveShimmer();
        }

        // TODO Load restore packages
        foreach (var catalog in allCatalogs)
        {
            var viewModel = _host.CreateInstance<PackageCatalogViewModel>(catalog);
            PackageCatalogs.Add(viewModel);
            CatalogLoaded?.Invoke(null, viewModel);
        }
    }

    /// <summary>
    /// Removes a package catalog shimmer
    /// </summary>
    public void RemoveShimmer()
    {
        if (PackageCatalogShimmers.Any())
        {
            PackageCatalogShimmers.Remove(PackageCatalogShimmers.Last());
        }
    }
}
