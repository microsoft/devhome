// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class SearchView : UserControl
{
    public SearchViewModel ViewModel => (SearchViewModel)DataContext;

    public SearchView()
    {
        this.InitializeComponent();
    }

    private void PackagesListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            for (var i = 0; i < listView.Items.Count; i++)
            {
                if (listView.ContainerFromIndex(i) is ListViewItem item && item.Content is PackageViewModel package)
                {
                    AutomationProperties.SetName(item, package.PackageTitle);
                }
            }
        }
    }
}
