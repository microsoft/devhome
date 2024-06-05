// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Services;
using DevHome.SetupFlow.Behaviors;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

public partial class PackageCatalogListViewModel : ObservableObject, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(PackageCatalogListViewModel));
    private readonly ICatalogDataSourceLoader _catalogDataSourceLoader;
    private readonly IExtensionService _extensionService;
    private readonly PackageCatalogViewModelFactory _packageCatalogViewModelFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SemaphoreSlim _loadCatalogsSemaphore = new(1, 1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CatalogFullPath))]
    private PackageCatalogViewModel _viewAllCatalog;
    private bool disposedValue;

    public List<string> CatalogFullPath => new()
    {
        AppManagementBehavior.Title,
        ViewAllCatalog?.Name ?? string.Empty,
    };

    /// <summary>
    /// Gets a list of package catalogs to display
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PackageCatalogViewModel> _packageCatalogs;

    /// <summary>
    /// Gets a list of shimmer indices.
    /// This list is used to repeat the shimmer control {Count} times
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<int> _packageCatalogShimmers;

    public PackageCatalogListViewModel(
        IExtensionService extensionService,
        ICatalogDataSourceLoader catalogDataSourceLoader,
        PackageCatalogViewModelFactory packageCatalogViewModelFactory,
        DispatcherQueue dispatcherQueue)
    {
        _extensionService = extensionService;
        _dispatcherQueue = dispatcherQueue;
        _catalogDataSourceLoader = catalogDataSourceLoader;
        _packageCatalogViewModelFactory = packageCatalogViewModelFactory;
    }

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    private async Task LoadCatalogsAsync()
    {
        // Prevent concurrent loading of catalogs
        await _loadCatalogsSemaphore.WaitAsync();
        try
        {
            ResetCatalogs();
            AddShimmers(_catalogDataSourceLoader.CatalogCount);
            await foreach (var dataSourceCatalogs in _catalogDataSourceLoader.LoadCatalogsAsync())
            {
                foreach (var catalog in dataSourceCatalogs)
                {
                    var catalogVM = await Task.Run(() => _packageCatalogViewModelFactory(catalog));
                    PackageCatalogs.Add(catalogVM);
                }

                RemoveShimmers(dataSourceCatalogs.Count);
            }

            // Remove any remaining shimmers:
            // This can happen if for example a catalog was detected but not
            // displayed (e.g. catalog with no packages to display)
            RemoveShimmers(PackageCatalogShimmers.Count);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to load catalogs.");
        }
        finally
        {
            _loadCatalogsSemaphore.Release();
        }
    }

    /// <summary>
    /// Reset package catalogs
    /// </summary>
    private void ResetCatalogs()
    {
        // Note: Create new observable collections instead of clearing existing
        // ones to ensure that the collections are not modified while binding
        // notification event handlers are being processed which can cause
        // "unspecified exception".
        PackageCatalogs = [];
        PackageCatalogShimmers = [];
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
        TelemetryFactory.Get<ITelemetry>().LogCritical("Apps_ViewAll_Event");
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
    private async Task OnLoadedAsync()
    {
        // Listen for extension changes
        _extensionService.OnExtensionsChanged += OnExtensionChangedAsync;

        // When the view is loaded, ensure we exit the view all packages mode
        ExitViewAllPackages();
        await LoadCatalogsAsync();
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        _extensionService.OnExtensionsChanged -= OnExtensionChangedAsync;
    }

    private async void OnExtensionChangedAsync(object sender, EventArgs e)
    {
        await _dispatcherQueue.EnqueueAsync(() => LoadCatalogsAsync());
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _loadCatalogsSemaphore.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
