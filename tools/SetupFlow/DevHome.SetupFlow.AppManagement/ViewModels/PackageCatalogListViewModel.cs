// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
public partial class PackageCatalogListViewModel : ObservableObject
{
    /// <summary>
    /// List of package catalogs to display
    /// </summary>
    [ObservableProperty]
    private IList<PackageCatalogViewModel> _packageCatalogs;

    /// <summary>
    /// Occurrs when a package catalog is loaded
    /// </summary>
    public event EventHandler<PackageCatalogViewModel> CatalogLoaded;

    /// <summary>
    /// Load the package catalogs to display
    /// </summary>
    public async Task LoadCatalogsAsync()
    {
        var catalogs = new List<PackageCatalogViewModel>();

        // TODO Load recommended packages
        // TODO Load restore packages
        foreach (var catalog in catalogs)
        {
            CatalogLoaded?.Invoke(null, catalog);
        }

        PackageCatalogs = catalogs;
        await Task.CompletedTask;
    }
}
