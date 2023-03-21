// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

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

    /// <summary>
    /// Occurs when one of the packages in this catalog has its IsSelected state changed
    /// </summary>
    public event EventHandler<PackageViewModel> PackageSelectionChanged;

    public PackageCatalogViewModel(IHost host, PackageCatalog packageCatalog)
    {
        _packageCatalog = packageCatalog;
        Packages = packageCatalog.Packages.Select(p =>
        {
            var packageViewModel = host.CreateInstance<PackageViewModel>(p);
            packageViewModel.SelectionChanged += (sender, eventArg) => PackageSelectionChanged?.Invoke(sender, eventArg);
            return packageViewModel;
        }).ToReadOnlyCollection();
    }

    [RelayCommand]
    private void AddAllPackages()
    {
        foreach (var package in Packages)
        {
            package.IsSelected = true;
        }
    }
}
