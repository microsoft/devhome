// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.RepoConfig.Views;

/// <summary>
/// Dialog to handle changing the clone path in the repo review screen.
/// </summary>
public sealed partial class EditClonePathDialog
{
    /// <summary>
    /// Gets or sets the view model to handle clone paths.
    /// </summary>
    public EditClonePathViewModel EditClonePathViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model that handles the dev drive.
    /// </summary>
    public EditDevDriveViewModel EditDevDriveViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model to handle the folder picker.
    /// </summary>
    public FolderPickerViewModel FolderPickerViewModel
    {
        get; set;
    }

    public EditClonePathDialog()
    {
        this.InitializeComponent();
        EditClonePathViewModel = new EditClonePathViewModel();
        EditDevDriveViewModel = new EditDevDriveViewModel();
        FolderPickerViewModel = new FolderPickerViewModel();
        IsPrimaryButtonEnabled = FolderPickerViewModel.ValidateCloneLocation();
    }

    /// <summary>
    /// Open up folder picker.
    /// </summary>
    private async void ChooseCloneLocationButton_Click(object sender, RoutedEventArgs e)
    {
        await FolderPickerViewModel.ChooseCloneLocation();
        IsPrimaryButtonEnabled = FolderPickerViewModel.ValidateCloneLocation();
    }

    /// <summary>
    /// Adds or removes the default dev drive.  This dev drive will be made at the loading screen.
    /// </summary>
    private void MakeNewDevDriveComboBox_Click(object sender, RoutedEventArgs e)
    {
        // Getting here means
        // 1. The user does not have any existing dev drives
        // 2. The user wants to clone to a new dev drive.
        // 3. The user un-checked this and does not want a new dev drive.
        var isChecked = (sender as CheckBox).IsChecked;
        if (isChecked.Value)
        {
            EditDevDriveViewModel.MakeDefaultDevDrive();
            FolderPickerViewModel.DisableBrowseButton();
        }
        else
        {
            EditDevDriveViewModel.RemoveNewDevDrive();
            FolderPickerViewModel.EnableBrowseButton();
        }
    }

    /// <summary>
    /// User wants to customize the default dev drive.
    /// </summary>
    private void CustomizeDevDriveHyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        EditDevDriveViewModel.PopDevDriveCustomizationAsync();
    }

    /// <summary>
    /// User left the clone location.  Validate the text.
    /// </summary>
    private void CloneLocationTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        IsPrimaryButtonEnabled = FolderPickerViewModel.ValidateCloneLocation();
    }
}
