// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Utilities;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

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

    public void MakeDefaultDevDrive()
    {
        // DevDrive SetToDefaults
        ShowCustomizeOption = Visibility.Visible;

        // TODO: More changes to stitch this up in the coming PRs
        var (result, devDrive) = _devDriveManager.GetNewDevDrive();
        if (result == DevDriveOperationResult.Successful)
        {
            DevDrive = devDrive;
        }
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
        ShowCustomizeOption = Visibility.Collapsed;
    }

    public string GetDriveDisplayName()
    {
        return "OS_VHD (" + DevDrive.DriveLetter + ":) [" + DevDrive.DriveSizeInBytes + "]";
    }

    /// <summary>
    /// Pops the dev drive customization window.
    /// Subscribe to an event that fires when the window is closed.
    /// </summary>
    public async void PopDevDriveCustomizationAsync()
    {
        var windowOpened = await _devDriveManager.LaunchDevDriveWindow(DevDrive);
        if (windowOpened)
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
        IsDevDriveCheckboxEnabled = true;
        DevDrive = devDrive;
    }
}
