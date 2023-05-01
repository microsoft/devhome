// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Utilities;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Represents the type of display name that should appear in the clone repo textbox
/// 1.DriveRootKind in the form of "D:\"
/// 2.FormattedKind in the form of "DriveLabel (DriveLetter:\) [Size in gigabytes] . e.g Dev Disk (D:\) [50 GB]
/// </summary>
public enum DevDriveDisplayNameKind
{
    DriveRootKind,
    FormattedDriveLabelKind,
}

/// <summary>
/// The view model to handle
/// 1. Customizing a dev drive, and
/// 2. Handling UI for showing dev drives.
/// </summary>
public partial class EditDevDriveViewModel : ObservableObject
{
    /// <summary>
    /// The manager to handle dev drives.
    /// </summary>
    private const char InvalidCharacterForDriveLetter = '\0';

    /// <summary>
    /// The manager to handle dev drives.
    /// </summary>
    private readonly IDevDriveManager _devDriveManager;

    /// <summary>
    /// Gets a value indicating whether the Dev Drive window is opened or closed.
    /// </summary>
    public bool IsWindowOpen
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the dev drive to make.
    /// </summary>
    public IDevDrive DevDrive
    {
        get; set;
    }

    private bool _canShowDevDriveUI;

    /// <summary>
    /// Gets a value indicating whether the the drive label, location, size or drive letter has changed when the Dev Drive window closes.
    /// </summary>
    public bool DevDriveDetailsChanged
    {
        get; private set;
    }

    /// <summary>
    /// Some builds don't have dev drives.
    /// </summary>
    [ObservableProperty]
    private Visibility _showDevDriveInformation;

    /// <summary>
    /// The customization hyperlink button visibility changes if the user wants a new dev drive.
    /// </summary>
    [ObservableProperty]
    private Visibility _showCustomizeOption;

    /// <summary>
    /// The checkbox can be disabled if the user has an existing dev drive.
    /// </summary>
    [ObservableProperty]
    private bool _isDevDriveCheckboxEnabled;

    [ObservableProperty]
    private bool _devDriveValidationError;

    public EditDevDriveViewModel(IDevDriveManager devDriveManager)
    {
        _devDriveManager = devDriveManager;
        ShowCustomizeOption = Visibility.Collapsed;
        IsDevDriveCheckboxEnabled = true;
    }

    public event EventHandler<string> DevDriveClonePathUpdated = (_, path) => { };

    public void ClonePathUpdated()
    {
        DevDriveClonePathUpdated(this, GetDriveDisplayName());
    }

    public bool MakeDefaultDevDrive()
    {
        // DevDrive SetToDefaults
        ShowCustomizeOption = Visibility.Visible;
        DevDrive = _devDriveManager.GetNewDevDrive();
        DevDriveValidationError = (DevDrive.State == DevDriveState.New) ? false : true;
        return DevDriveValidationError;
    }

    public void ShowDevDriveUIIfEnabled()
    {
        if (_canShowDevDriveUI)
        {
            ShowDevDriveInformation = Visibility.Visible;
        }
    }

    public void HideDevDriveUI()
    {
        ShowDevDriveInformation = Visibility.Collapsed;
    }

    public void RemoveNewDevDrive()
    {
        _devDriveManager.RequestToCloseDevDriveWindow(DevDrive);
        _devDriveManager.CancelChangesToDevDrive();
        DevDrive = null;
        ShowCustomizeOption = Visibility.Collapsed;
        DevDriveValidationError = false;
    }

    /// <summary>
    /// Get the display name for the Dev Drive. By default no arguments will return the Rooth path of the Dev Drive
    /// this is in the form of "DriveLetter:\" e.g D:\
    /// </summary>
    public string GetDriveDisplayName(DevDriveDisplayNameKind useDriveLetterOnly = DevDriveDisplayNameKind.DriveRootKind)
    {
        if (DevDrive.DriveLetter == InvalidCharacterForDriveLetter)
        {
            return string.Empty;
        }

        if (useDriveLetterOnly == DevDriveDisplayNameKind.DriveRootKind)
        {
            // Uses the actual place where we'll be cloning to
            return $@"{DevDrive.DriveLetter}:\";
        }

        var size = DevDriveUtil.ConvertBytesToString(DevDrive.DriveSizeInBytes);

        // For the case when an explicit terminating character is left at the end.
        if (size.Last() == '\0')
        {
            size = size.Remove(size.Length - 1);
        }

        return $@"{DevDrive.DriveLabel} ({DevDrive.DriveLetter}:) [{size}]";
    }

    /// <summary>
    /// Pops the dev drive customization window.
    /// Subscribe to an event that fires when the window is closed.
    /// </summary>
    public async void PopDevDriveCustomizationAsync()
    {
        if (IsWindowOpen)
        {
            return;
        }

        IsWindowOpen = await _devDriveManager.LaunchDevDriveWindow(DevDrive);
        if (IsWindowOpen)
        {
            _devDriveManager.ViewModelWindowClosed += DevDriveCustomizationWindowClosed;
            IsDevDriveCheckboxEnabled = false;
        }
    }

    /// <summary>
    /// Checks to see if a dev drive is on the users system.
    /// If a dev drive is found hide dev drive UI.
    /// </summary>
    /// <remarks>
    /// Won't show dev drive UI in 2 cases
    /// 1. Build does not support Dev Drive
    /// 2. User has existing dev drives.
    /// </remarks>
    public void SetUpStateIfDevDrivesIfExists()
    {
        ShowDevDriveInformation = DevDriveUtil.IsDevDriveFeatureEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (ShowDevDriveInformation == Visibility.Visible)
        {
            var existingDevDrives = _devDriveManager.GetAllDevDrivesThatExistOnSystem();
            if (existingDevDrives.Any())
            {
                ShowDevDriveInformation = Visibility.Collapsed;
                DevDrive = existingDevDrives.OrderByDescending(x => x.DriveSizeInBytes).First();
                _canShowDevDriveUI = false;
                return;
            }
        }
        else
        {
            _canShowDevDriveUI = false;
            return;
        }

        _canShowDevDriveUI = true;
    }

    /// <summary>
    /// Clean up when the customization window is closed.
    /// </summary>
    private void DevDriveCustomizationWindowClosed(object sender, IDevDrive devDrive)
    {
        IsWindowOpen = false;
        IsDevDriveCheckboxEnabled = true;
        DevDriveDetailsChanged = DevDriveChanged(devDrive);
        DevDrive = devDrive;
        ClonePathUpdated();
    }

    public bool IsDevDriveValid()
    {
        var results = _devDriveManager.GetDevDriveValidationResults(DevDrive);
        return results.Contains(DevDriveValidationResult.Successful);
    }

    /// <summary>
    /// Checks whether the information inside the Dev Drive has changed.
    /// </summary>
    /// <param name="newDevDrive"> the new Dev Drive object that will replace the original Dev Drive</param>
    /// <returns>Return bool stating whether the Dev Drive info has changed</returns>
    private bool DevDriveChanged(IDevDrive newDevDrive)
    {
        if (DevDrive == null)
        {
            return true;
        }

        if (!string.Equals(DevDrive.DriveLocation, newDevDrive.DriveLocation, StringComparison.Ordinal))
        {
            return true;
        }
        else if (!string.Equals(DevDrive.DriveLabel, newDevDrive.DriveLabel, StringComparison.Ordinal))
        {
            return true;
        }
        else if (DevDrive.DriveLetter != newDevDrive.DriveLetter)
        {
            return true;
        }
        else if (DevDrive.DriveSizeInBytes != newDevDrive.DriveSizeInBytes)
        {
            return true;
        }

        return false;
    }
}
