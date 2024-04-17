// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;
using Serilog;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Class for providing and caching a subset of package view models in order to
/// maintain a consistent package state on the UI. This service should be
/// accessed by the UI thread.
/// </summary>
/// <remarks>
/// For example, if a package was selected from a search result and then the
/// same package appeared in another search query, that package should remain
/// selected, even if the underlying WinGet COM object in memory is different.
/// Furthermore, if the same package appears in popular apps section, the
/// selection should also be reflected in that section and everywhere else on
/// the UI. The same behavior is expected when unselecting the package.
/// </remarks>
public class PackageProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(PackageProvider));

    private sealed class PackageCache
    {
        /// <summary>
        /// Gets or sets the cached package view model
        /// </summary>
        public PackageViewModel PackageViewModel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the package
        /// should be cached temporarily or permanently.
        /// </summary>
        public bool IsPermanent { get; set; }
    }

    private readonly PackageViewModelFactory _packageViewModelFactory;
    private readonly object _lock = new();

    /// <summary>
    /// Dictionary for caching package view models
    /// </summary>
    /// <remarks>
    /// - Permanently cache packages from catalogs (e.g. Popular apps, restore apps)
    ///   * Example: A package in popular apps section that also appears in
    ///     restore apps section and in a search query should have the same
    ///     visual state when selected or unselected.
    /// - Temporarily cache packages that are selected from the search result,
    ///   and remove them once unselected.
    ///   * Example: A package that appears in the two different search queries
    ///     should have the same visual state when selected or unselected.
    /// </remarks>
    private readonly Dictionary<PackageUniqueKey, PackageCache> _packageViewModelCache = new();

    /// <summary>
    /// Observable collection containing the list of selected packages in the
    /// order they were added
    /// </summary>
    private readonly ObservableCollection<PackageViewModel> _selectedPackages = new();

    /// <summary>
    /// Gets a read-only wrapper around the selected package observable collection
    /// </summary>
    public ReadOnlyObservableCollection<PackageViewModel> SelectedPackages => new(_selectedPackages);

    /// <summary>
    /// Occurs when a package selection has changed
    /// </summary>
    public event EventHandler SelectedPackagesItemChanged;

    public PackageProvider(PackageViewModelFactory packageViewModelFactory)
    {
        _packageViewModelFactory = packageViewModelFactory;
    }

    /// <summary>
    /// Create a package view model if one does not exist already in the cache,
    /// otherwise return the one form the cache
    /// </summary>
    /// <param name="package">WinGet package model</param>
    /// <param name="cachePermanently">
    /// True, if the package should be cached permanently.
    /// False, if the package should not be cached.
    /// </param>
    /// <returns>Package view model</returns>
    public PackageViewModel CreateOrGet(IWinGetPackage package, bool cachePermanently = false)
    {
        lock (_lock)
        {
            // Check if package is cached
            if (_packageViewModelCache.TryGetValue(package.UniqueKey, out var value))
            {
                // Promote to permanent cache if requested
                _log.Debug($"Package [{package.Id}] is cached; returning");
                value.IsPermanent = value.IsPermanent || cachePermanently;
                return value.PackageViewModel;
            }

            // Package is not cached, create a new one
            _log.Debug($"Creating view model for package [{package.Id}]");
            var viewModel = _packageViewModelFactory(package);
            viewModel.SelectionChanged += OnPackageSelectionChanged;
            viewModel.VersionChanged += OnSelectedPackageVersionChanged;

            // Cache if requested
            if (cachePermanently)
            {
                _log.Debug($"Caching package {package.Id}");
                _packageViewModelCache.TryAdd(package.UniqueKey, new PackageCache()
                {
                    PackageViewModel = viewModel,
                    IsPermanent = true,
                });
            }

            return viewModel;
        }
    }

    private void OnSelectedPackageVersionChanged(object sender, string version)
    {
        var packageViewModel = sender as PackageViewModel;
        if (packageViewModel?.IsSelected == true)
        {
            // Notify subscribers that an item in the list of selected packages has changed
            SelectedPackagesItemChanged?.Invoke(packageViewModel, EventArgs.Empty);
        }
    }

    private void OnPackageSelectionChanged(object sender, bool isSelected)
    {
        if (sender is not PackageViewModel packageViewModel)
        {
            return;
        }

        lock (_lock)
        {
            if (packageViewModel.IsSelected)
            {
                _log.Information($"Package [{packageViewModel.Package.Id}] has been selected");

                // If a package is selected and is not already cached permanently,
                // cache it temporarily
                _log.Debug($"Caching package [{packageViewModel.Package.Id}]");
                _packageViewModelCache.TryAdd(packageViewModel.UniqueKey, new PackageCache()
                {
                    PackageViewModel = packageViewModel,
                    IsPermanent = false,
                });

                // Add to the selected package collection
                _selectedPackages.Add(packageViewModel);
            }
            else
            {
                _log.Information($"Package [{packageViewModel.Package.Id}] has been un-selected");

                // If a package is unselected and is cached temporarily, remove it
                // from the cache
                if (_packageViewModelCache.TryGetValue(packageViewModel.UniqueKey, out var value) && !value.IsPermanent)
                {
                    _log.Debug($"Removing package [{packageViewModel.Package.Id}] from cache");
                    _packageViewModelCache.Remove(packageViewModel.UniqueKey);
                    packageViewModel.SelectionChanged -= OnPackageSelectionChanged;
                    packageViewModel.VersionChanged -= OnSelectedPackageVersionChanged;
                }

                // Remove from the selected package collection
                _selectedPackages.Remove(packageViewModel);
            }
        }

        // Notify subscribers that an item in the list of selected packages has changed
        SelectedPackagesItemChanged?.Invoke(packageViewModel, EventArgs.Empty);
    }

    /// <summary>
    /// Clear cache and selected packages
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            // Clear cache
            _log.Debug($"Clearing package view model cache");
            _packageViewModelCache.Clear();

            // Clear list of selected packages
            _log.Information($"Clearing selected packages");
            _selectedPackages.Clear();
        }
    }
}
