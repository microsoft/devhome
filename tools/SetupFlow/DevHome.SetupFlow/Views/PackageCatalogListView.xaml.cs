// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.AppManagement.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.AppManagement.Views;
public sealed partial class PackageCatalogListView : UserControl
{
    public PackageCatalogListViewModel ViewModel => (PackageCatalogListViewModel)DataContext;

    public PackageCatalogListView()
    {
        this.InitializeComponent();
    }
}
