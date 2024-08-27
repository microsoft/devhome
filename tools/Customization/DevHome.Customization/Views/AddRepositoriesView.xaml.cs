// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.Models;
using DevHome.Customization.ViewModels;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Customization.Views;

public sealed partial class AddRepositoriesView : UserControl
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AddRepositoriesView));

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

    private void SourceControlProviderMenuFlyout_Opening(object sender, object e)
    {
        if (sender is MenuFlyout menuFlyout)
        {
            menuFlyout.Items.Clear();

            foreach (var extension in ViewModel.ExtensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository).Result)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = extension.ExtensionDisplayName,
                    Tag = extension,
                };
                menuItem.Click += AssignSourceControlProviderButton_Click;
                ToolTipService.SetToolTip(menuItem, extension.PackageDisplayName);
                menuFlyout.Items.Add(menuItem);
            }
        }
    }

    public void AssignSourceControlProviderButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract relevant data from view and give to view model for assign
        MenuFlyoutItem menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is RepositoryInformation repoInfo)
        {
            ViewModel.AssignSourceControlProviderToRepository(menuItem.Tag as IExtensionWrapper, repoInfo.RepositoryRootPath);
        }
    }

    public void OpenFolderInFileExplorer_Click(object sender, RoutedEventArgs e)
    {
        MenuFlyoutItem? menuItem = sender as MenuFlyoutItem;
        if (menuItem?.DataContext is RepositoryInformation repoInfo)
        {
            try
            {
                // Open folder in file explorer
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = repoInfo.RepositoryRootPath,
                };

                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open folder in file explorer");
            }
        }
    }
}
