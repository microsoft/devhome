// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Windows.FileDialog;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View Model to handle the folder picker.
/// </summary>
public partial class FolderPickerViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(FolderPickerViewModel));

    private readonly ISetupFlowStringResource _stringResource;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    /// <summary>
    /// Some pages don't show a folder picker.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowFolderPicker;

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

    [ObservableProperty]
    private string _folderPickerErrorMessage;

    [ObservableProperty]
    private bool _showFolderPickerError;

    public FolderPickerViewModel(ISetupFlowStringResource stringResource, SetupFlowOrchestrator setupFlowOrchestrator)
    {
        _stringResource = stringResource;
        ShouldShowFolderPicker = true;
        CloneLocation = string.Empty;
        IsBrowseButtonEnabled = true;
        _setupFlowOrchestrator = setupFlowOrchestrator;
        SetDefaultCloneLocation();
    }

    public void ShowFolderPicker()
    {
        _log.Information("Showing folder picker");
        ShouldShowFolderPicker = true;
    }

    public void CloseFolderPicker()
    {
        _log.Information("Closing folder picker");
        ShouldShowFolderPicker = false;
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
            InDevDriveScenario = false;
            CloneLocationAlias = string.Empty;
            CloneLocation = maybeCloneLocation.FullName;
        }

        EnableBrowseButton();
    }

    /// <summary>
    /// Opens the directory picker
    /// </summary>
    /// <returns>An awaitable task.</returns>
    private async Task<DirectoryInfo> PickCloneDirectoryAsync()
    {
        try
        {
            _log.Information("Opening folder picker to select clone directory");
            using var folderPicker = new WindowOpenFolderDialog();
            var locationToCloneTo = await folderPicker.ShowAsync(Application.Current.GetService<Window>());
            if (locationToCloneTo != null && locationToCloneTo.Path.Length > 0)
            {
                _log.Information($"Selected '{locationToCloneTo.Path}' as location to clone to");
                return new DirectoryInfo(locationToCloneTo.Path);
            }
            else
            {
                _log.Information("Didn't select a location to clone to");
                return null;
            }
        }
        catch (Exception e)
        {
            _log.Error(e, "Failed to open folder picker");
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
        if (string.IsNullOrEmpty(CloneLocation) || string.IsNullOrWhiteSpace(CloneLocation))
        {
            ShowFolderPickerError = false;
            return false;
        }

        if (!Path.IsPathFullyQualified(CloneLocation))
        {
            FolderPickerErrorMessage = _stringResource.GetLocalized(StringResourceKey.ClonePathNotFullyQualifiedMessage);
            ShowFolderPickerError = true;
            return false;
        }

        if (!InDevDriveScenario)
        {
            // User could enter a path that does not exist.  That is okay.  Clone will make the path.
            // If the location does exist, make sure it does not point to a file.
            if (File.Exists(CloneLocation))
            {
                FolderPickerErrorMessage = _stringResource.GetLocalized(StringResourceKey.ClonePathNotFolder);
                ShowFolderPickerError = true;
                return false;
            }

            // User could put in a drive letter that does not exist.
            var drive = Path.GetPathRoot(CloneLocation);
            if (!Directory.Exists(drive))
            {
                FolderPickerErrorMessage = _stringResource.GetLocalized(StringResourceKey.ClonePathDriveDoesNotExist);
                ShowFolderPickerError = true;
                return false;
            }
        }

        ShowFolderPickerError = false;
        return true;
    }

    /// <summary>
    /// Sets the clone location to the default location. For the local machine setup the default is the user's
    /// profile. For the target machine setup the default is the common documents folder, since we don't know
    /// which folders exists on the target machine.
    /// </summary>
    private void SetDefaultCloneLocation()
    {
        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (_setupFlowOrchestrator.CurrentSetupFlowKind == SetupFlowKind.SetupTarget)
        {
            userFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        }

        CloneLocation = Path.Join(userFolder, "source", "repos");
    }
}
