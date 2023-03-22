// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.DevDrive.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.StartScreen;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

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
    private readonly IDevDriveManager _devDriveManager;

    public bool IsWindowOpened
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
        var (result, devDrive) = _devDriveManager.GetNewDevDrive();
        if (result == DevDriveOperationResult.Successful)
        {
            DevDrive = devDrive;
            return true;
        }

        return false;
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
        ShowCustomizeOption = Visibility.Collapsed;
    }

    /// <summary>
    /// Get the display name for the Dev Drive. By default no arguments will return the Rooth path of the Dev Drive
    /// this is in the form of "DriveLetter:\" e.g D:\
    /// </summary>
    public string GetDriveDisplayName(DevDriveDisplayNameKind useDriveLetterOnly = DevDriveDisplayNameKind.DriveRootKind)
    {
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
        if (IsWindowOpened)
        {
            return;
        }

        IsWindowOpened = await _devDriveManager.LaunchDevDriveWindow(DevDrive);
        if (IsWindowOpened)
        {
            _devDriveManager.ViewModelWindowClosed += DevDriveCustomizationWindowClosed;
            IsDevDriveCheckboxEnabled = false;
        }
    }

    /// <summary>
    /// Checks to see if a dev drive is on the users system.
    /// If a dev drive is found hide dev drive UI.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    /// <remarks>
    /// Won't show dev drive UI in 2 cases
    /// 1. Build does not support Dev Drive
    /// 2. User has existing dev drives.
    /// </remarks>
    public async Task SetUpStateIfDevDrivesIfExistsAsync()
    {
        ShowDevDriveInformation = DevDriveUtil.IsDevDriveFeatureEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (ShowDevDriveInformation == Visibility.Visible)
        {
            var existingDevDrives = await _devDriveManager.GetAllDevDrivesThatExistOnSystem();
            if (existingDevDrives.Any())
            {
                ShowDevDriveInformation = Visibility.Collapsed;
                DevDrive = existingDevDrives.OrderByDescending(x => x.DriveSizeInBytes).First();
                _canShowDevDriveUI = false;
            }
        }
        else
        {
            _canShowDevDriveUI = false;
        }

        _canShowDevDriveUI = true;
    }

    /// <summary>
    /// Clean up when the customization window is closed.
    /// </summary>
    private void DevDriveCustomizationWindowClosed(object sender, IDevDrive devDrive)
    {
        if (devDrive.ID == DevDrive.ID)
        {
            IsWindowOpened = false;
            IsDevDriveCheckboxEnabled = true;
            DevDrive = devDrive;
            ClonePathUpdated();
        }
    }
}
