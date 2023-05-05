// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Delegate factory for creating package catalog view models
/// </summary>
/// <param name="catalog">Package catalog</param>
/// <returns>Package catalog view model</returns>
public delegate PackageCatalogViewModel PackageCatalogViewModelFactory(PackageCatalog catalog);

/// <summary>
/// ViewModel class for a <see cref="PackageCatalog"/> model.
/// </summary>
public partial class PackageCatalogViewModel : ObservableObject
{
    private readonly PackageCatalog _packageCatalog;

    [ObservableProperty]
    private bool _canAddAllPackages;

    public string Name => _packageCatalog.Name;

    public string Description => _packageCatalog.Description;

    public IReadOnlyCollection<PackageViewModel> Packages { get; private set; }

    public PackageCatalogViewModel(PackageProvider packageProvider, PackageCatalog packageCatalog)
    {
        _packageCatalog = packageCatalog;
        Packages = packageCatalog.Packages
            .Select(p => packageProvider.CreateOrGet(p, cachePermanently: true))
            .OrderBy(p => p.IsInstalled)
            .ToReadOnlyCollection();
    }

    [RelayCommand]
    private void AddAllPackages()
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Adding all packages from catalog {Name} to selection");
        foreach (var package in Packages)
        {
            package.IsSelected = true;
        }
    }
}
