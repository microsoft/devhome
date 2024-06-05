// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.SetupFlow.Views;

public sealed partial class ReviewView : UserControl
{
    public ReviewView()
    {
        this.InitializeComponent();
    }

    public ReviewViewModel ViewModel => (ReviewViewModel)this.DataContext;

    private void ReviewNavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationView navView && navView.MenuItemsSource is IEnumerable menuItems)
        {
            foreach (var item in menuItems)
            {
                if (navView.ContainerFromMenuItem(item) is NavigationViewItem navViewItem && navViewItem.Content is ReviewTabViewModelBase reviewTab)
                {
                    AutomationProperties.SetName(navViewItem, reviewTab.TabTitle);
                }
            }
        }
    }

    private void GenerateConfigInfoButton_Click(object sender, RoutedEventArgs e)
    {
        GenerateConfigInfoTeachingTip.IsOpen = !GenerateConfigInfoTeachingTip.IsOpen;
    }

    private void GenerateConfigInfoButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        GenerateConfigInfoButton_Click(sender, e);
    }
}
