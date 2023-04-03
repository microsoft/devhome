// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
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
        var devDrive = addRepoDialog.EditDevDriveViewModel.DevDrive;

        if (addRepoDialog.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(devDrive);
        }

        var everythingToClone = addRepoDialog.AddRepoViewModel.EverythingToClone;
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.SaveSetupTaskInformation(everythingToClone);

            // We currently only support adding either a local path or a Dev Drive as the cloning location. Only one can be selected
            // during the add repo dialog flow. So if multiple repositories are selected and the user chose to clone them to the Dev Drive
            // then we make sure all the locations will clone to that Dev Drive.
            if (devDrive != null)
            {
                foreach (var cloneInfo in everythingToClone)
                {
                    cloneInfo.CloneToDevDrive = true;
                    cloneInfo.CloneLocationAlias = addRepoDialog.FolderPickerViewModel.CloneLocationAlias;
                }

                // The cloning location may have changed e.g The original Drive clone path for Dev Drives was the F: drive for items
                // on the add repo page, but during the Add repo dialog flow the user chose to change this location to the D: drive.
                // we need to reflect this for all the old items currently in the add repo page.
                ViewModel.UpdateCollectionWithDevDriveInfo(everythingToClone[0]);
                ViewModel.DevDriveManager.IncreaseRepositoriesCount(everythingToClone.Count);
                ViewModel.DevDriveManager.ConfirmChangesToDevDrive();
            }
        }
        else
        {
            // User cancelled the dialog, Report back to the Dev drive Manager to revert any changes.
            ViewModel.ReportDialogCancellation();
        }
    }

    /// <summary>
    /// User wants to edit the clone location of a repo.  Show the dialog.
    /// </summary>
    /// <param name="sender">Used to find the cloning information clicked on.</param>
    private async void EditClonePathButton_Click(object sender, RoutedEventArgs e)
    {
        var cloningInformation = (sender as Button).DataContext as CloningInformation;
        var oldLocation = cloningInformation.CloningLocation;
        var wasCloningToDevDrive = cloningInformation.CloneToDevDrive;
        var editClonePathDialog = new EditClonePathDialog(ViewModel.DevDriveManager, cloningInformation);
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        editClonePathDialog.XamlRoot = RepoConfigStackPanel.XamlRoot;
        editClonePathDialog.RequestedTheme = themeService.Theme;
        var result = await editClonePathDialog.ShowAsync(ContentDialogPlacement.InPlace);

        var devDrive = editClonePathDialog.EditDevDriveViewModel.DevDrive;
        cloningInformation.CloneToDevDrive = devDrive != null;

        if (result == ContentDialogResult.Primary)
        {
            cloningInformation.CloningLocation = new System.IO.DirectoryInfo(editClonePathDialog.FolderPickerViewModel.CloneLocation);
            ViewModel.UpdateCollection();

            // User intended to clone to Dev Drive before launching dialog but now they are not,
            // so decrease the Dev Managers count.
            if (wasCloningToDevDrive && !cloningInformation.CloneToDevDrive)
            {
                ViewModel.DevDriveManager.DecreaseRepositoriesCount();
                ViewModel.DevDriveManager.CancelChangesToDevDrive();
            }

            if (cloningInformation.CloneToDevDrive)
            {
                ViewModel.DevDriveManager.ConfirmChangesToDevDrive();

                // User switched from local path to Dev Drive
                if (!wasCloningToDevDrive)
                {
                    ViewModel.DevDriveManager.IncreaseRepositoriesCount(1);
                }

                cloningInformation.CloneLocationAlias = editClonePathDialog.FolderPickerViewModel.CloneLocationAlias;
            }
        }
        else
        {
            // User cancelled the dialog, Report back to the Dev drive Manager to revert any changes.
            ViewModel.ReportDialogCancellation();
            cloningInformation.CloneToDevDrive = wasCloningToDevDrive;
        }

        if (editClonePathDialog.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(editClonePathDialog.EditDevDriveViewModel.DevDrive);
        }

        // If the user launches the edit button, and changes or updates the clone path to be a Dev Drive, we need
        // to update the other entries in the list, that are being cloned to the Dev Drive with this new information.
        if (oldLocation != cloningInformation.CloningLocation && cloningInformation.CloneToDevDrive)
        {
            ViewModel.UpdateCollectionWithDevDriveInfo(cloningInformation);
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
        if (cloningInformation.CloneToDevDrive)
        {
            ViewModel.DevDriveManager.DecreaseRepositoriesCount();
        }
    }
}
