// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.Models;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class AddRepositoriesView : UserControl
{
    public FileExplorerViewModel ViewModel
    {
        get;
    }

    public AddRepositoriesView()
    {
        ViewModel = Application.Current.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
    }

    public void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract relevant data from view and give to view model for remove
        MenuFlyoutItem menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is RepositoryInformation repoInfo)
        {
            ViewModel.RemoveTrackedRepositoryFromDevHome(repoInfo.RepositoryRootPath);
        }
    }

    public void AssignSourceControlProviderButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract relevant data from view and give to view model for assign
        MenuFlyoutItem menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is RepositoryInformation repoInfo)
        {
            ViewModel.AssignSourceControlProviderToRepository(menuItem.Text, repoInfo.RepositoryRootPath);
        }
    }
}
