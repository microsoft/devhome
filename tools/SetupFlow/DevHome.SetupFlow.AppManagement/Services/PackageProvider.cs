// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement.Services;

public class PackageProvider
{
    private class PackageCache
    {
        public PackageViewModel PackageViewModel { get; set; }

        public bool IsTemporary { get; set; }
    }

    private readonly IHost _host;
    private readonly Dictionary<PackageUniqueKey, PackageCache> _packageViewModelCache = new ();
    private readonly ObservableCollection<PackageViewModel> _selectedPackages = new ();

    public ReadOnlyObservableCollection<PackageViewModel> SelectedPackages => new (_selectedPackages);

    public event EventHandler<PackageViewModel> PackageSelectionChanged;

    public PackageProvider(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Create a package view model if one does not exist already in the cache,
    /// otherwise return the one form the cache
    /// </summary>
    /// <param name="package">WinGet package model</param>
    /// <param name="cache">Set to true if the package should be cached permanently</param>
    /// <returns>Package view model</returns>
    public PackageViewModel CreateOrGet(IWinGetPackage package, bool cache = false)
    {
        // Check if package is cached
        if (_packageViewModelCache.TryGetValue(package.UniqueKey, out var value))
        {
            return value.PackageViewModel;
        }

        // Package is not cached, create a new one and cache if necessary
        var viewModel = _host.CreateInstance<PackageViewModel>(package);
        viewModel.SelectionChanged += OnPackageSelectionChanged;

        if (cache)
        {
            // Cache package view model permanently
            _packageViewModelCache.TryAdd(package.UniqueKey, new PackageCache()
            {
                PackageViewModel = viewModel,
                IsTemporary = false,
            });
        }

        return viewModel;
    }

    public void OnPackageSelectionChanged(object sender, PackageViewModel packageViewModel)
    {
        if (packageViewModel.IsSelected)
        {
            if (!_packageViewModelCache.ContainsKey(packageViewModel.UniqueKey))
            {
                _packageViewModelCache.TryAdd(packageViewModel.UniqueKey, new PackageCache()
                {
                    PackageViewModel = packageViewModel,
                    IsTemporary = true,
                });
            }
        }
        else
        {
            if (_packageViewModelCache.TryGetValue(packageViewModel.UniqueKey, out var value) && value.IsTemporary)
            {
                _packageViewModelCache.Remove(packageViewModel.UniqueKey);
            }
        }

        PackageSelectionChanged?.Invoke(null, packageViewModel);
    }

    /// <summary>
    /// Clear cache and selected packages
    /// </summary>
    public void Clear()
    {
        _packageViewModelCache.Clear();
        _selectedPackages.Clear();
    }
}
