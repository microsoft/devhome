// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DevHome.SetupFlow.Views;

public sealed partial class PackageCatalogListView : UserControl
{
    public PackageCatalogListViewModel ViewModel => (PackageCatalogListViewModel)DataContext;

    public PackageCatalogListView()
    {
        this.InitializeComponent();
    }

    private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        // Add a separator between consecutive items
        if (args.Element is Border border)
        {
            if (args.Index == 0)
            {
                border.Padding = new Thickness(0);
                border.BorderThickness = new Thickness(0);
            }
            else
            {
                border.Padding = new Thickness(0, 30, 0, 0);
                border.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["ControlStrokeColorDefault"]);
                border.BorderThickness = new Thickness(0, 1, 0, 0);
            }
        }
    }
}
