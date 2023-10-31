// Copyright (c) Microsoft Corporation and Contributors.
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
        var listView = sender as ListView;
        if (listView != null)
        {
            for (var i = 0; i < listView.Items.Count; i++)
            {
                var item = listView.ContainerFromIndex(i) as ListViewItem;
                if (item != null)
                {
                    if (item.Content is PackageViewModel package)
                    {
                        AutomationProperties.SetName(item, package.PackageTitle);
                    }
                }
            }
        }
    }
}
