// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.Helpers;
using DevHome.Customization.Models;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
            var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
            menuFlyout.Items.Clear();

            foreach (var extension in ViewModel.ExtensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository).Result)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = extension.ExtensionDisplayName,
                    Tag = extension,
                };
                menuItem.Click += AssignSourceControlProviderButton_Click;

                ToolTipService.SetToolTip(menuItem, stringResource.GetLocalized("PrefixForDevHomeVersion", extension.PackageDisplayName));
                menuFlyout.Items.Add(menuItem);
            }

            var unassignMenuItem = new MenuFlyoutItem
            {
                Text = stringResource.GetLocalized("MenuFlyoutUnregisteredRepository_Content"),
            };
            unassignMenuItem.Click += UnassignFolderButton_Click;
            menuFlyout.Items.Add(unassignMenuItem);
        }
    }

    public void UnassignFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract relevant data from view and give to view model for unassign
        MenuFlyoutItem menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is RepositoryInformation repoInfo)
        {
            ViewModel.UnassignSourceControlProviderFromRepository(repoInfo.RepositoryRootPath);
        }
    }

    public async void AssignSourceControlProviderButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract relevant data from view and give to view model for assign
        MenuFlyoutItem menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is RepositoryInformation repoInfo)
        {
            var taskResult = await ViewModel.AssignSourceControlProviderToRepository(menuItem.Tag as IExtensionWrapper, repoInfo.RepositoryRootPath);
            if (taskResult?.Result != Helpers.ResultType.Success)
            {
                _log.Error("Error occurred while assigning source control provider: ", taskResult?.Error, taskResult?.Exception, taskResult?.DiagnosticText, taskResult?.DisplayMessage);
                ShowErrorContentDialog(taskResult!, this.XamlRoot);
            }
        }
    }

    public async void ShowErrorContentDialog(SourceControlValidationResult result, XamlRoot xamlRoot)
    {
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        var errorDialog = new ContentDialog
        {
            Title = stringResource.GetLocalized("AssignSourceControlErrorDialog_Title"),
            Content = result.DisplayMessage,
            CloseButtonText = stringResource.GetLocalized("CloseButtonText"),
            XamlRoot = xamlRoot,
            RequestedTheme = ActualTheme,
        };

        // Set automation properties
        AutomationProperties.SetName(errorDialog, stringResource.GetLocalized("AssignSourceControlErrorDialog_Title"));
        AutomationProperties.SetHeadingLevel(errorDialog, AutomationHeadingLevel.Level1);

        _ = await errorDialog.ShowAsync();
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
