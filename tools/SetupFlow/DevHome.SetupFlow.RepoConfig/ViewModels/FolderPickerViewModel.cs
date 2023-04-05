// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

/// <summary>
/// View Model to handle the folder picker.
/// </summary>
public partial class FolderPickerViewModel : ObservableObject
{
    /// <summary>
    /// Some pages don't show a folder picker.
    /// </summary>
    [ObservableProperty]
    private Visibility _shouldShowFolderPicker;

    /// <summary>
    /// The clone location the repos should be cloned to.
    /// </summary>
    [ObservableProperty]
    private string _cloneLocation;

    /// <summary>
    /// The clone location alias, this is for displaying the formatted Dev Drive text. While allowing
    /// the clone location to only have the a path inside of it.
    /// </summary>
    [ObservableProperty]
    private string _cloneLocationAlias;

    /// <summary>
    /// Browse button can be disabled if the user checked to make a new dev drive.
    /// </summary>
    [ObservableProperty]
    private bool _isBrowseButtonEnabled;

    /// <summary>
    /// Used to show different content in the textbox based on whether the checkbox is checked or unchecked.
    /// </summary>
    /// <remarks>
    /// When true shows the clone path textbox in a readonly state with the Dev Drives clone location alias.
    /// When false the clone path's text box is enabled and the user can type/copy and paste a
    /// path into the textbox freely. For dev drives we clone to the root of the drive.
    /// </remarks>
    [ObservableProperty]
    private bool _inDevDriveScenario;

    public FolderPickerViewModel()
    {
        ShouldShowFolderPicker = Visibility.Visible;
        CloneLocation = string.Empty;
        IsBrowseButtonEnabled = true;
    }

    public void ShowFolderPicker()
    {
        ShouldShowFolderPicker = Visibility.Visible;
    }

    public void CloseFolderPicker()
    {
        ShouldShowFolderPicker = Visibility.Collapsed;
    }

    public void EnableBrowseButton()
    {
        IsBrowseButtonEnabled = true;
    }

    public void DisableBrowseButton()
    {
        IsBrowseButtonEnabled = false;
    }

    public void SetCloneLocation(string cloneLocation)
    {
        CloneLocation = cloneLocation;
    }

    /// <summary>
    ///   Opens the directory picker and saves the location if a location was chosen.
    /// </summary>
    public async Task ChooseCloneLocation()
    {
        DisableBrowseButton();
        var maybeCloneLocation = await PickCloneDirectoryAsync();
        if (maybeCloneLocation != null)
        {
            CloneLocation = maybeCloneLocation.FullName;
            CloneLocationAlias = string.Empty;
            InDevDriveScenario = false;
        }

        EnableBrowseButton();
    }

    /// <summary>
    /// Opens the directory picker
    /// </summary>
    /// <returns>An awaitable task.</returns>
    private async Task<DirectoryInfo> PickCloneDirectoryAsync()
    {
        var folderPicker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Application.Current.GetService<WindowEx>().GetWindowHandle());
        folderPicker.FileTypeFilter.Add("*");

        var locationToCloneTo = await folderPicker.PickSingleFolderAsync();
        if (locationToCloneTo != null && locationToCloneTo.Path.Length > 0)
        {
            return new DirectoryInfo(locationToCloneTo.Path);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Makes sure the clone location is not null and is rooted.
    /// </summary>
    /// <returns>True if clone location is good.  Otherwise false.</returns>
    public bool ValidateCloneLocation()
    {
        // Make sure clone location is filled in and is fully qualified.
        if (string.IsNullOrEmpty(CloneLocation) || string.IsNullOrWhiteSpace(CloneLocation) || !Path.IsPathFullyQualified(CloneLocation))
        {
            CloneLocation = string.Empty;
            return false;
        }

        return true;
    }
}
