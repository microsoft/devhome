// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
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
    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

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

    /// <summary>
    /// Gets a value indicating whether we should show the Dev Drive Checkbox in the UI.
    /// The Dev Drive checkbox is only shown when a Dev Drive does not already exist on the system.
    /// </summary>
    public bool CanShowDevDriveUI
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the drive label, location, size or drive letter has changed when the Dev Drive window closes.
    /// </summary>
    public bool DevDriveDetailsChanged
    {
        get; private set;
    }

    /// <summary>
    /// Some builds don't have dev drives.
    /// </summary>
    [ObservableProperty]
    private bool _showDevDriveInformation;

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

    /// <summary>
    /// Boolean to control whether the Dev Drive checkbox is checked or unchecked.
    /// </summary>
    [ObservableProperty]
    private bool _isDevDriveCheckboxChecked;

    [ObservableProperty]
    private bool _devDriveValidationError;

    public EditDevDriveViewModel(IDevDriveManager devDriveManager, SetupFlowOrchestrator setupFlowOrchestrator)
    {
        _devDriveManager = devDriveManager;
        ShowCustomizeOption = Visibility.Collapsed;
        IsDevDriveCheckboxEnabled = true;
        _setupFlowOrchestrator = setupFlowOrchestrator;
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
        if (CanShowDevDriveUI)
        {
            ShowDevDriveInformation = true;
        }
    }

    public void HideDevDriveUI()
    {
        ShowDevDriveInformation = false;
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
    /// Get the display name for the Dev Drive. By default no arguments will return the root path of the Dev Drive
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
            // Uses the actual place to clone to
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
    public async Task PopDevDriveCustomizationAsync()
    {
        if (IsWindowOpen)
        {
            return;
        }

        IsWindowOpen = await _devDriveManager.LaunchDevDriveWindow(DevDrive);
        if (IsWindowOpen)
        {
            IsDevDriveCheckboxEnabled = false;

            // Convert the wait for closed event into an async task
            TaskCompletionSource<IDevDrive> devDriveWindowTask = new();
            EventHandler<IDevDrive> eventHandler = (_, devDrive) => devDriveWindowTask.SetResult(devDrive);
            _devDriveManager.ViewModelWindowClosed += eventHandler;
            var devDriveFromWindow = await devDriveWindowTask.Task;
            _devDriveManager.ViewModelWindowClosed -= eventHandler;

            IsWindowOpen = false;
            IsDevDriveCheckboxEnabled = true;
            DevDriveDetailsChanged = DevDriveChanged(devDriveFromWindow);
            DevDrive = devDriveFromWindow;
            DevDriveValidationError = (DevDrive.State == DevDriveState.New) ? false : true;
            ClonePathUpdated();
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
        ShowDevDriveInformation = DevDriveUtil.IsDevDriveFeatureEnabled && _setupFlowOrchestrator.IsSettingUpLocalMachine;
        if (ShowDevDriveInformation)
        {
            var existingDevDrives = _devDriveManager.GetAllDevDrivesThatExistOnSystem();
            if (existingDevDrives.Any())
            {
                ShowDevDriveInformation = false;
                DevDrive = existingDevDrives.OrderByDescending(x => x.DriveSizeInBytes).First();
                CanShowDevDriveUI = false;
                return;
            }
        }
        else
        {
            CanShowDevDriveUI = false;
            return;
        }

        CanShowDevDriveUI = true;
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
