// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.ViewModels;
using Microsoft.Extensions.Hosting;
using PackageCompositeKey = System.ValueTuple<string, string>;

namespace DevHome.SetupFlow.AppManagement.Services;

public class PackageProvider
{
    private readonly IHost _host;
    private readonly Dictionary<PackageCompositeKey, (PackageViewModel package, bool keep)> _packageViewModelCache = new ();
    private readonly ObservableCollection<PackageViewModel> _selectedPackages = new ();

    public ReadOnlyObservableCollection<PackageViewModel> SelectedPackages => new (_selectedPackages);

    public PackageProvider(IHost host)
    {
        _host = host;
    }

    public PackageViewModel CreateOrGet(IWinGetPackage package, bool cache = false)
    {
        // Check if package is cached
        if (_packageViewModelCache.TryGetValue(package.CompositeKey, out var value))
        {
            return value.package;
        }

        // Package is not cached, create a new one and cache if necessary
        var viewModel = _host.CreateInstance<PackageViewModel>(package);
        viewModel.SelectionChanged += OnPackageSelectionChanged;
        if (cache)
        {
            _packageViewModelCache.TryAdd(package.CompositeKey, (package: viewModel, keep: true));
        }

        return viewModel;
    }

    public void OnPackageSelectionChanged(object sender, PackageViewModel packageViewModel)
    {
        if (packageViewModel.IsSelected)
        {
            _selectedPackages.Add(packageViewModel);
            if (!_packageViewModelCache.ContainsKey(packageViewModel.CompositeKey))
            {
                _packageViewModelCache.TryAdd(packageViewModel.CompositeKey, (package: packageViewModel, keep: false));
            }
        }
        else
        {
            _selectedPackages.Remove(packageViewModel);
            if (_packageViewModelCache.TryGetValue(packageViewModel.CompositeKey, out var value) && !value.keep)
            {
                _packageViewModelCache.Remove(packageViewModel.CompositeKey);
            }
        }
    }

    public void Clear()
    {
        _packageViewModelCache.Clear();
        _selectedPackages.Clear();
    }
}
