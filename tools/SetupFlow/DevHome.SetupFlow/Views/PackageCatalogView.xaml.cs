// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

[INotifyPropertyChanged]
public sealed partial class PackageCatalogView : UserControl
{
    [ObservableProperty]
    private List<PackageViewModel> _displayPackages;

    /// <summary>
    /// Gets or sets the package catalog to display
    /// </summary>
    public PackageCatalogViewModel Catalog
    {
        get => (PackageCatalogViewModel)GetValue(CatalogProperty);
        set => SetValue(CatalogProperty, value);
    }

    public int PackageCount
    {
        get => (int)GetValue(PackageCountProperty);
        set => SetValue(PackageCountProperty, value);
    }

    public ICommand ViewAllCommand
    {
        get => (ICommand)GetValue(ViewAllCommandProperty);
        set => SetValue(PackageCountProperty, value);
    }

    public PackageCatalogView()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Update the list of package group cache
    /// </summary>
    private void UpdatePackageGroups()
    {
        if (Catalog != null)
        {
            DisplayPackages = Catalog.Packages.Take(PackageCount).ToList();
        }
    }

    public static readonly DependencyProperty CatalogProperty = DependencyProperty.Register(nameof(Catalog), typeof(PackageCatalogViewModel), typeof(PackageCatalogView), new PropertyMetadata(null, (c, _) => ((PackageCatalogView)c).UpdatePackageGroups()));
    public static readonly DependencyProperty PackageCountProperty = DependencyProperty.Register(nameof(PackageCount), typeof(int), typeof(PackageCatalogView), new PropertyMetadata(4, (c, _) => ((PackageCatalogView)c).UpdatePackageGroups()));
    public static readonly DependencyProperty ViewAllCommandProperty = DependencyProperty.Register(nameof(ViewAllCommand), typeof(ICommand), typeof(PackageCatalogView), new PropertyMetadata(null));
}
