// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.RepoConfig.Views;

/// <summary>
/// Shows the user the repositories they have sleected.
/// </summary>
public sealed partial class RepoConfigView : UserControl
{
    public RepoConfigView()
    {
        this.InitializeComponent();
    }

    public RepoConfigViewModel ViewModel => (RepoConfigViewModel)this.DataContext;

    /// <summary>
    /// User wants to add a repo.  Bring up the tool.
    /// </summary>
    private async void AddRepoButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var addRepoDialog = new AddRepoDialog(ViewModel.DevDriveManager);

        await addRepoDialog.GetPluginsAsync();
        await addRepoDialog.SetupDevDrivesAsync();
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        addRepoDialog.XamlRoot = RepoConfigStackPanel.XamlRoot;
        addRepoDialog.RequestedTheme = themeService.Theme;
        var result = await addRepoDialog.ShowAsync(ContentDialogPlacement.InPlace);

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.SaveSetupTaskInformation(addRepoDialog.AddRepoViewModel.EverythingToClone);
        }

        if (addRepoDialog.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(addRepoDialog.EditDevDriveViewModel.DevDrive);
        }
    }

    /// <summary>
    /// User wants to edit the clone location of a repo.  Show the dialog.
    /// </summary>
    /// <param name="sender">Used to find the cloning information clicked on.</param>
    private async void EditClonePathButton_Click(object sender, RoutedEventArgs e)
    {
        var editClonePathDialog = new EditClonePathDialog(ViewModel.DevDriveManager);
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        editClonePathDialog.XamlRoot = RepoConfigStackPanel.XamlRoot;
        editClonePathDialog.RequestedTheme = themeService.Theme;
        var result = await editClonePathDialog.ShowAsync(ContentDialogPlacement.InPlace);

        if (result == ContentDialogResult.Primary)
        {
            var cloningInformation = (sender as Button).DataContext as CloningInformation;
            cloningInformation.CloningLocation = new System.IO.DirectoryInfo(editClonePathDialog.FolderPickerViewModel.CloneLocation);
            ViewModel.UpdateCollection();
        }

        if (editClonePathDialog.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(editClonePathDialog.EditDevDriveViewModel.DevDrive);
        }
    }

    /// <summary>
    /// Removes a repository to clone from the list.
    /// </summary>
    /// <param name="sender">Used to find the cloning information clicked on.</param>
    private void RemoveCloningInformationButton_Click(object sender, RoutedEventArgs e)
    {
        var cloningInformation = (sender as Button).DataContext as CloningInformation;
        ViewModel.RemoveCloningInformation(cloningInformation);
    }
}
