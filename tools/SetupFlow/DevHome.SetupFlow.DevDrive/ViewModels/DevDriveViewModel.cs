// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Utilities;
using DevHome.SetupFlow.DevDrive.Windows;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.System;
using WinUIEx;

namespace DevHome.SetupFlow.DevDrive.ViewModels;

public partial class DevDriveViewModel : ObservableObject, IDevDriveWindowViewModel
{
    private readonly ILogger _logger;
    private readonly IStringResource _stringResource;
    private readonly DevDriveTaskGroup _taskGroup;
    private readonly IDevDriveManager _devDriveManager;
    private readonly Models.DevDrive _concreteDevDrive;
    private readonly string _localizedBrowseButtonText;

    // TODO: This icon is subject to change, when Dev Home gets a new icon along with a more global way to
    // access it since its not set programmatically currently, only through xaml.
    private readonly string _devHomeIconPath = "Assets/WindowIcon.ico";
    private readonly Dictionary<ByteUnit, string> _byteUnitList;

    /// <summary>
    /// Gets the window that will contain the view.
    /// </summary>
    public DevDriveWindow DevDriveWindowContainer
    {
        get; private set;
    }

    public Dictionary<ByteUnit, string> ByteUnitList => _byteUnitList;

    public IDevDrive AssociatedDrive => _concreteDevDrive;

    public string AppImage => Path.Combine(AppContext.BaseDirectory, _devHomeIconPath);

    public string AppTitle => Application.Current.GetService<WindowEx>().Title;

    public DevDriveViewModel(
        IHost host,
        ILogger logger,
        IStringResource stringResource,
        DevDriveTaskGroup taskGroup,
        Models.DevDrive devDrive)
    {
        _logger = logger;
        _taskGroup = taskGroup;
        _stringResource = stringResource;
        _devDriveManager = host.GetService<IDevDriveManager>();
        _concreteDevDrive = devDrive;
        _byteUnitList = new Dictionary<ByteUnit, string>
        {
            { ByteUnit.GB, stringResource.GetLocalized(StringResourceKey.DevDriveWindowByteUnitComboBoxGB) },
            { ByteUnit.TB, stringResource.GetLocalized(StringResourceKey.DevDriveWindowByteUnitComboBoxTB) },
        };
        _localizedBrowseButtonText = _stringResource.GetLocalized(StringResourceKey.BrowseTextBlock);
        Size = DevDriveUtil.MinSizeForGbComboBox;
        DriveLabel = devDrive.DriveLabel;
        Location = devDrive.DriveLocation;
        ComboBoxDriveLetter = devDrive.DriveLetter;
    }

    /// <summary>
    /// Gets or Sets the value in the Dev Drive name textbox.
    /// This name will be used as the label for the eventual Dev Drive.
    /// This is limited to 32 characters (a Windows limitation).
    /// </summary>
    [ObservableProperty]
    private string _driveLabel;

    /// <summary>
    /// Gets or Sets the value in the Dev Drive location textbox.
    /// This is the location that we will save the virtual disk file to.
    /// </summary>
    [ObservableProperty]
    private string _location;

    /// <summary>
    /// Gets or Sets the Dev Drive size. This is the size the Dev Drive will be created with. This along with
    /// the value selected in the byteUnitList will tell us the exact size the user wants to create
    /// their Dev Drive to have e.g if this value is 50 and the byteUnitList is GB, the user wants the drive to be 50 GB in size.
    /// </summary>
    [ObservableProperty]
    private double _size;

    [ObservableProperty]
    private bool _invalidDriveLabelErrorOccurred;
    [ObservableProperty]
    private bool _notEnoughFreeSpaceErrorOccurred;
    [ObservableProperty]
    private bool _invalidDriveSizeErrorOccurred;
    [ObservableProperty]
    private bool _noDriveLettersAvailableErrorOccurred;
    [ObservableProperty]
    private bool _invalidFolderLocationErrorOccurred;

    /// <summary>
    /// Byte unit of mearsure combo box.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(MinimumAllowedSize))]
    [NotifyPropertyChangedFor(nameof(MaximumAllowedSize))]
    [ObservableProperty]
    private int _comboBoxByteUnit;

    /// <summary>
    /// Gets or sets Drive letter in combo box.
    /// </summary>
    [ObservableProperty]
    private char _comboBoxDriveLetter;

    /// <summary>
    /// Gets the drive letters available on the system but have not already been marked for
    /// creation by the Dev Drive Manager.
    /// </summary>
    public IList<char> DriveLetters => _devDriveManager.GetAvailableDriveLetters(AssociatedDrive);

    /// <summary>
    /// Gets the maximum size allowed in the Number box based
    /// on the value currently selected in _comboBoxByteUnit.
    /// </summary>
    public double MaximumAllowedSize
    {
        get
        {
            if ((ByteUnit)_comboBoxByteUnit == ByteUnit.TB)
            {
                return DevDriveUtil.MaxSizeForTbComboBox;
            }

            return DevDriveUtil.MaxSizeForGbComboBox;
        }
    }

    /// <summary>
    /// Gets the minimumize size allowed in the Number box based
    /// on the value currently selected in _comboBoxByteUnit.
    /// </summary>
    public double MinimumAllowedSize
    {
        get
        {
            if ((ByteUnit)_comboBoxByteUnit == ByteUnit.TB)
            {
                return DevDriveUtil.MinSizeForTbComboBox;
            }

            return DevDriveUtil.MinSizeForGbComboBox;
        }
    }

    public string LocalizedBrowseButtonText => _localizedBrowseButtonText;

    /// <summary>
    /// Opens folder picker and adds folder to the drive location, if the user does not cancel the dialog.
    /// </summary>
    [RelayCommand]
    public async void ChooseFolderLocation()
    {
        var folderPicker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, DevDriveWindowContainer.GetWindowHandle());
        folderPicker.FileTypeFilter.Add("*");

        var location = await folderPicker.PickSingleFolderAsync();
        if (location != null && location.Path.Length > 0)
        {
            Location = location.Path;
            OnPropertyChanged(nameof(Location));
        }
    }

    /// <summary>
    /// Cancel button click command used to close the window. Note, the only time the  <see cref="Models.DevDrive"/>
    /// object associated with the view model should have its values changed is on a save command.
    /// </summary>
    [RelayCommand]
    private void CancelButton()
    {
        DevDriveWindowContainer.Close();
    }

    /// <summary>
    /// Opens the Windows settings app and redirects the user to the disks and volumes page.
    /// </summary>
    [RelayCommand]
    public async void LaunchDisksAndVolumesSettingsPage()
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
    }

    private void ResetErrors()
    {
        InvalidDriveSizeErrorOccurred = false;
        NotEnoughFreeSpaceErrorOccurred = false;
        InvalidDriveLabelErrorOccurred = false;
        InvalidFolderLocationErrorOccurred = false;
        NoDriveLettersAvailableErrorOccurred = false;
    }

    /// <summary>
    /// Save button click command sends the <see cref="Models.DevDrive"/> associated with
    /// with the view model to the  <see cref="DevDriveManager"/> to have its contents validated
    /// Obly when the validation is successful do we close the window when the save button is clicked.
    /// </summary>
    [RelayCommand]
    private void SaveButton()
    {
        ResetErrors();
        ByteUnit driveUnitOfMeasure = (ByteUnit)_comboBoxByteUnit;
        var tempDrive = new Models.DevDrive()
        {
            DriveLetter = ComboBoxDriveLetter,
            DriveSizeInBytes = DevDriveUtil.ConvertToBytes(Size, driveUnitOfMeasure),
            DriveUnitOfMeasure = driveUnitOfMeasure,
            DriveLocation = Location,
            DriveLabel = _driveLabel,
        };

        Models.DevDrive.SwapContent(tempDrive, _concreteDevDrive);
        var validation = _devDriveManager.GetDevDriveValidationResults(_concreteDevDrive);
        if (validation.Contains(DevDriveOperationResult.Successful))
        {
            _concreteDevDrive.State = DevDriveState.New;
            DevDriveWindowContainer.Close();
        }
        else
        {
            // Validation failed, we need to swap the contents back to the original.
            Models.DevDrive.SwapContent(tempDrive, _concreteDevDrive);
            ShowErrorInUI(validation);
        }
    }

    public Task<bool> LaunchDevDriveWindow()
    {
        DevDriveWindowContainer = new (this);
        DevDriveWindowContainer.Closed += ViewContainerClosed;
        return Task.FromResult(DevDriveWindowContainer.Show());
    }

    private void ViewContainerClosed(object sender, WindowEventArgs args)
    {
        _devDriveManager.NotifyDevDriveWindowClosed(_concreteDevDrive);
    }

    public void ShowErrorInUI(ISet<DevDriveOperationResult> resultSet)
    {
        if (resultSet.Contains(DevDriveOperationResult.InvalidDriveSize))
        {
            InvalidDriveSizeErrorOccurred = true;
        }

        if (resultSet.Contains(DevDriveOperationResult.NotEnoughFreeSpace))
        {
            NotEnoughFreeSpaceErrorOccurred = true;
        }

        if (resultSet.Contains(DevDriveOperationResult.InvalidDriveLabel))
        {
            InvalidDriveLabelErrorOccurred = true;
        }

        if (resultSet.Contains(DevDriveOperationResult.InvalidFolderLocation))
        {
            InvalidFolderLocationErrorOccurred = true;
        }

        if (resultSet.Contains(DevDriveOperationResult.NoDriveLettersAvailable))
        {
            NoDriveLettersAvailableErrorOccurred = true;
        }
    }
}
