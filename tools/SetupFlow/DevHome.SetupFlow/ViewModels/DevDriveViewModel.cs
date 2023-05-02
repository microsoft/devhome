// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.TelemetryEvents;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using DevHome.SetupFlow.Windows;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Windows.Globalization.NumberFormatting;
using Windows.Storage.Pickers;
using Windows.System;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class DevDriveViewModel : ObservableObject, IDevDriveWindowViewModel
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IDevDriveManager _devDriveManager;
    private readonly string _localizedBrowseButtonText;
    private readonly ObservableCollection<string> _fileNameAndSizeErrorList = new ();

    private readonly string _devHomeIconPath = "Assets/DevHome.ico";
    private readonly Dictionary<ByteUnit, string> _byteUnitList;

    private Models.DevDrive _concreteDevDrive = new ();
    private DevDriveTaskGroup _taskGroup;

    /// <summary>
    /// Gets a value indicating whether the DevDrive window has been opened.
    /// </summary>
    public bool IsDevDriveWindowOpen
    {
        get; private set;
    }

    /// <summary>
    /// Gets the window that will contain the view.
    /// </summary>
    public DevDriveWindow DevDriveWindowContainer
    {
        get; private set;
    }

    /// <summary>
    /// Gets the decimal formatter that will allow us to take only whole numbers in the number box.
    /// </summary>
    public DecimalFormatter DevDriveDecimalFormatter
    {
        get
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            rounder.Increment = 1;
            rounder.RoundingAlgorithm = RoundingAlgorithm.RoundTowardsZero;
            DecimalFormatter formatter = new DecimalFormatter();
            formatter.IntegerDigits = 1;
            formatter.FractionDigits = 0;
            formatter.NumberRounder = rounder;
            return formatter;
        }
    }

    /// <summary>
    /// Gets the dictionary mapping between a ByteUnit and the the localized string it represents.
    /// </summary>
    public Dictionary<ByteUnit, string> ByteUnitList => _byteUnitList;

    /// <summary>
    /// Gets or sets the value indicating the IDevDrive object for the view model.
    /// </summary>
    public IDevDrive AssociatedDrive
    {
        get => _concreteDevDrive;
        set => _concreteDevDrive = new Models.DevDrive(value);
    }

    /// <summary>
    /// Gets a value indicating the path to the app icon for the secondary window.
    /// </summary>
    public string AppImage => Path.Combine(AppContext.BaseDirectory, _devHomeIconPath);

    /// <summary>
    /// Gets a value indicating the window title of the Dev Drive window.
    /// </summary>
    public string AppTitle => Application.Current.GetService<WindowEx>().Title;

    public DevDriveViewModel(
        ISetupFlowStringResource stringResource,
        DevDriveTaskGroup taskGroup,
        IDevDriveManager devDriveManager)
    {
        _taskGroup = taskGroup;
        _stringResource = stringResource;
        _byteUnitList = new Dictionary<ByteUnit, string>
        {
            { ByteUnit.GB, stringResource.GetLocalized(StringResourceKey.DevDriveWindowByteUnitComboBoxGB) },
            { ByteUnit.TB, stringResource.GetLocalized(StringResourceKey.DevDriveWindowByteUnitComboBoxTB) },
        };
        _localizedBrowseButtonText = _stringResource.GetLocalized(StringResourceKey.BrowseTextBlock);
        _devDriveManager = devDriveManager;
        _devDriveManager.RequestToCloseViewModelWindow += CloseRequestedDevDriveWindow;
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
    public char ComboBoxDriveLetter
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether we should show the the localized error text for when the folder location the user wants to save the virtual disk to is not found.
    /// </summary>
    [ObservableProperty]
    private string _invalidFolderLocationError;

    /// <summary>
    /// Gets or sets a value indicating whether we should show the localized error text for when there are no drive letters to assign to a Dev Drive.
    /// </summary>
    [ObservableProperty]
    private string noDriveLettersAvailableError;

    /// <summary>
    /// Gets the drive letters available on the system and is not already in use by a Dev Drive
    /// that the Dev Drive manager is holding in memory.
    /// </summary>
    public IList<char> DriveLetters => _devDriveManager.GetAvailableDriveLetters(AssociatedDrive.DriveLetter);

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

    public DevDriveTaskGroup TaskGroup
    {
        get => _taskGroup;
        set => _taskGroup = value;
    }

    /// <summary>
    /// gets the localized Browse button text for the browse button.
    /// </summary>
    public string LocalizedBrowseButtonText => _localizedBrowseButtonText;

    /// <summary>
    /// gets the list of localized error text for filename errors and sizing errors that will be shown in the UI.
    /// </summary>
    public ObservableCollection<string> FileNameAndSizeErrorList => _fileNameAndSizeErrorList;

    /// <summary>
    /// Opens folder picker and adds folder to the drive location, if the user does not cancel the dialog.
    /// </summary>
    [RelayCommand]
    public async void ChooseFolderLocation()
    {
        Log.Logger?.ReportInfo(Log.Component.DevDrive, "Opening file picker to select dev drive location");
        var folderPicker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, DevDriveWindowContainer.GetWindowHandle());
        folderPicker.FileTypeFilter.Add("*");

        var location = await folderPicker.PickSingleFolderAsync();
        if (!string.IsNullOrWhiteSpace(location?.Path))
        {
            Log.Logger?.ReportInfo(Log.Component.DevDrive, $"Selected Dev Drive location: {location.Path}");
            Location = location.Path;
            OnPropertyChanged(nameof(Location));
        }
        else
        {
            Log.Logger?.ReportInfo(Log.Component.DevDrive, "No location selected for Dev Drive");
        }
    }

    /// <summary>
    /// Closes the Dev Drive window if an object that holds a reference to the AssociatedDrive requests
    /// for the window to be closed.
    /// </summary>
    public void CloseRequestedDevDriveWindow(object sender, IDevDrive devDrive)
    {
        if (IsDevDriveWindowOpen)
        {
            Log.Logger?.ReportInfo(Log.Component.DevDrive, "Closing dev drive window");
            DevDriveWindowContainer.Close();
        }
    }

    /// <summary>
    /// Updates the associated Dev Drive and property fields for the UI with new details from another
    /// Dev Drive.
    /// </summary>
    public void UpdateDevDriveInfo(IDevDrive devDrive)
    {
        Log.Logger?.ReportInfo(Log.Component.DevDrive, "Updating Dev Drive info");
        AssociatedDrive = devDrive;
        if (devDrive.DriveSizeInBytes > DevDriveUtil.MinDevDriveSizeInBytes)
        {
            ByteUnit byteUnit;
            (Size, byteUnit) = DevDriveUtil.ConvertFromBytes(devDrive.DriveSizeInBytes);
            ComboBoxByteUnit = (int)byteUnit;
        }
        else
        {
            _concreteDevDrive.DriveSizeInBytes = DevDriveUtil.MinDevDriveSizeInBytes;
            Size = DevDriveUtil.MinSizeForGbComboBox;
        }

        DriveLabel = devDrive.DriveLabel;
        Location = devDrive.DriveLocation;
        ComboBoxDriveLetter = devDrive.DriveLetter;
        _taskGroup.AddDevDriveTask(AssociatedDrive);
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
        TelemetryFactory.Get<ITelemetry>().Log(
            "LaunchDisksAndVolumesSettingsPageTriggered",
            LogLevel.Measure,
            new DisksAndVolumesSettingsPageTriggeredEvent(source: "DevDriveView"));
        await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
    }

    /// <summary>
    /// Save button click command sends the <see cref="Models.DevDrive"/> associated with
    /// with the view model to the  <see cref="DevDriveManager"/> to have its contents validated
    /// Obly when the validation is successful do we close the window when the save button is clicked.
    /// </summary>
    [RelayCommand]
    private void SaveButton()
    {
        Log.Logger?.ReportInfo(Log.Component.DevDrive, "Saving changes to Dev Drive");
        ByteUnit driveUnitOfMeasure = (ByteUnit)_comboBoxByteUnit;
        var tempDrive = new Models.DevDrive()
        {
            DriveLetter = ComboBoxDriveLetter,
            DriveSizeInBytes = DevDriveUtil.ConvertToBytes(Size, driveUnitOfMeasure),
            DriveUnitOfMeasure = driveUnitOfMeasure,
            DriveLocation = Location,
            DriveLabel = _driveLabel,
            ID = _concreteDevDrive.ID,
        };

        var validation = _devDriveManager.GetDevDriveValidationResults(tempDrive);
        if (validation.Contains(DevDriveValidationResult.Successful))
        {
            _concreteDevDrive = tempDrive;
            _concreteDevDrive.State = DevDriveState.New;
            _taskGroup.AddDevDriveTask(AssociatedDrive);
            DevDriveWindowContainer.Close();
        }
        else
        {
            ShowErrorInUI(validation);
        }
    }

    /// <summary>
    /// Allows the view model to remove the current tasks related to creating
    /// a Dev drive.
    /// </summary>
    public void RemoveTasks()
    {
        _taskGroup.RemoveDevDriveTasks();
    }

    /// <summary>
    /// Launches a secondary window that holds the page content inside DevDriveView.xaml
    /// </summary>
    public Task<bool> LaunchDevDriveWindow()
    {
        Log.Logger?.ReportInfo(Log.Component.DevDrive, "Launching window to set up Dev Drive");
        ResetErrors();
        DevDriveWindowContainer = new (this);
        DevDriveWindowContainer.Closed += ViewContainerClosed;
        DevDriveWindowContainer.Activate();
        IsDevDriveWindowOpen = true;

        // If state is invalid then show errors in the UI as soon as we launch the window.
        if (_concreteDevDrive.State != DevDriveState.New)
        {
            ShowErrorInUI(_devDriveManager.GetDevDriveValidationResults(_concreteDevDrive));
        }

        return Task.FromResult(IsDevDriveWindowOpen);
    }

    /// <summary>
    /// Signals to the DevDrive manager to let the object who originally requested to launch the
    /// Dev Drive window that the window has closed.
    /// </summary>
    private void ViewContainerClosed(object sender, WindowEventArgs args)
    {
        IsDevDriveWindowOpen = false;
        _devDriveManager.NotifyDevDriveWindowClosed(_concreteDevDrive);
    }

    /// <summary>
    /// Shows the user all errors found after clicking the save button.
    /// </summary>
    public void ShowErrorInUI(ISet<DevDriveValidationResult> resultSet)
    {
        var prefix = "DevDrive";
        var tempfileNameAndSizeErrorList = new List<string>();
        NoDriveLettersAvailableError = string.Empty;
        InvalidFolderLocationError = string.Empty;
        foreach (DevDriveValidationResult result in resultSet)
        {
            Log.Logger?.ReportError(Log.Component.DevDrive, $"Input validation Error in Dev Drive window: {result.ToString()}");
            var errorString = _stringResource.GetLocalized(prefix + result.ToString());

            if (result == DevDriveValidationResult.NoDriveLettersAvailable)
            {
                NoDriveLettersAvailableError = errorString;
            }
            else if (result == DevDriveValidationResult.InvalidFolderLocation)
            {
                InvalidFolderLocationError = errorString;
            }
            else if (result == DevDriveValidationResult.InvalidDriveLabel ||
                result == DevDriveValidationResult.NotEnoughFreeSpace ||
                result == DevDriveValidationResult.FileNameAlreadyExists)
            {
                tempfileNameAndSizeErrorList.Add(errorString);
            }
        }

        if (tempfileNameAndSizeErrorList.Count == 0 || tempfileNameAndSizeErrorList.Count == 1)
        {
            FileNameAndSizeErrorList.Clear();
        }

        // Remove errors from list that were resolved.
        for (var i = FileNameAndSizeErrorList.Count - 1; i >= 0; i--)
        {
            if (!tempfileNameAndSizeErrorList.Contains(FileNameAndSizeErrorList[i]))
            {
                FileNameAndSizeErrorList.Remove(FileNameAndSizeErrorList[i]);
            }
        }

        // Add new errors to the list.
        foreach (var error in tempfileNameAndSizeErrorList)
        {
            if (!FileNameAndSizeErrorList.Contains(error))
            {
                FileNameAndSizeErrorList.Add(error);
            }
        }
    }

    /// <summary>
    /// Resets errors shown in the UI
    /// </summary>
    private void ResetErrors()
    {
        NoDriveLettersAvailableError = string.Empty;
        InvalidFolderLocationError = string.Empty;
        FileNameAndSizeErrorList.Clear();
    }
}
