// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.Windows.FileDialog;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using DevHome.SetupFlow.Windows;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Globalization.NumberFormatting;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class DevDriveViewModel : ObservableObject, IDevDriveWindowViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevDriveViewModel));
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IDevDriveManager _devDriveManager;
    private readonly IHost _host;
    private readonly string _localizedBrowseButtonText;
    private readonly string _devHomeIconPath = "Assets/DevHome.ico";
    private readonly Dictionary<ByteUnit, string> _byteUnitList;
    private readonly Guid _activityId;

    private Models.DevDrive _concreteDevDrive = new();
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
    /// Gets the decimal formatter that will format the value in the numberbox. RoundHalfTowardsZero is used
    /// because the SFBS_FLAGS_TRUNCATE_UNDISPLAYED_DECIMAL_DIGITS flag in <see cref="DevDriveUtil.ConvertBytesToString"/> to
    /// get the formatted Drive size. RoundHalfTowardsZero will effectively truncate all values after the hundredth position.
    /// </summary>
    public DecimalFormatter DevDriveDecimalFormatter
    {
        get
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            rounder.Increment = 0.01;
            rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfTowardsZero;
            DecimalFormatter formatter = new DecimalFormatter();
            formatter.IntegerDigits = 1;
            formatter.FractionDigits = 2;
            formatter.NumberRounder = rounder;
            return formatter;
        }
    }

    /// <summary>
    /// Gets the dictionary mapping between a ByteUnit and the localized string it represents.
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
        IDevDriveManager devDriveManager,
        IHost host,
        SetupFlowOrchestrator setupFlowOrchestrator)
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
        RefreshDriveLetterToSizeMapping();
        PropertyChanged += (_, args) => ValidatePropertyByName(args.PropertyName);
        _host = host;
        _activityId = setupFlowOrchestrator.ActivityId;
    }

    /// <summary>
    /// Gets or Sets the value in the Dev Drive name textbox.
    /// This name will be used as the label for the eventual Dev Drive.
    /// This is limited to 32 characters (a Windows limitation).
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveButtonCommand))]
    private string _driveLabel;

    /// <summary>
    /// Gets or Sets the value in the Dev Drive location textbox.
    /// This is the location that we will save the virtual disk file to.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveButtonCommand))]
    private string _location;

    /// <summary>
    /// Gets or Sets the Dev Drive size. This is the size the Dev Drive will be created with. This along with
    /// the value selected in the byteUnitList will tell us the exact size the user wants to create
    /// their Dev Drive to have e.g if this value is 50 and the byteUnitList is GB, the user wants the drive to be 50 GB in size.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveButtonCommand))]
    private double _size;

    /// <summary>
    /// Byte unit of measure combo box.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(MinimumAllowedSize))]
    [NotifyPropertyChangedFor(nameof(MaximumAllowedSize))]
    [NotifyCanExecuteChangedFor(nameof(SaveButtonCommand))]
    [ObservableProperty]
    private int _comboBoxByteUnit;

    /// <summary>
    /// Gets or sets Drive letter in combo box.
    /// </summary>
    [NotifyCanExecuteChangedFor(nameof(SaveButtonCommand))]
    [ObservableProperty]
    private char? _comboBoxDriveLetter;

    /// <summary>
    /// Gets or sets a value indicating whether the localized error text for when there is an error in the folder location should be shown.
    /// </summary>
    [ObservableProperty]
    private DevDriveValidationResult? _folderLocationError;

    /// <summary>
    /// Gets or sets a value indicating whether the localized error text for when there is an error retrieving a drive letter for the user should be shown
    /// </summary>
    [ObservableProperty]
    private DevDriveValidationResult? _driveLetterError;

    /// <summary>
    /// Gets or sets the drive letters available on the system and is not already in use by a Dev Drive
    /// that the Dev Drive manager is holding in memory.
    /// </summary>
    public List<char> DriveLetters { get; set; } = new();

    /// <summary>
    /// Gets the maximum size allowed in the Number box based
    /// on the value currently selected in _comboBoxByteUnit.
    /// </summary>
    public double MaximumAllowedSize
    {
        get
        {
            ValidateDriveSize();
            if ((ByteUnit)ComboBoxByteUnit == ByteUnit.TB)
            {
                return DevDriveUtil.MaxSizeForTbComboBox;
            }

            return DevDriveUtil.MaxSizeForGbComboBox;
        }
    }

    /// <summary>
    /// Gets the minimum size allowed in the Number box based
    /// on the value currently selected in _comboBoxByteUnit.
    /// </summary>
    public double MinimumAllowedSize
    {
        get
        {
            ValidateDriveSize();
            if ((ByteUnit)ComboBoxByteUnit == ByteUnit.TB)
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
    /// Gets or sets a Dictionary where the keys are current drive letters in use by the system and the values are the free space in bytes the drives have available.
    /// </summary>
    public Dictionary<char, ulong> DriveLetterToSizeMapping { get; set; } = new();

    /// <summary>
    /// Gets the localized Browse button text for the browse button.
    /// </summary>
    public string LocalizedBrowseButtonText => _localizedBrowseButtonText;

    /// <summary>
    /// Gets the list of DevDriveValidationResults that will be converted to localized text and shown in error info bars in the UI.
    /// </summary>
    public ObservableCollection<DevDriveValidationResult> FileNameAndSizeErrorList { get; } = new();

    /// <summary>
    /// Opens folder picker and adds folder to the drive location, if the user does not cancel the dialog.
    /// </summary>
    [RelayCommand]
    public async Task ChooseFolderLocationAsync()
    {
        try
        {
            _log.Information("Opening file picker to select dev drive location");
            using var folderPicker = new WindowOpenFolderDialog();
            var location = await folderPicker.ShowAsync(DevDriveWindowContainer);
            if (!string.IsNullOrWhiteSpace(location?.Path))
            {
                _log.Information($"Selected Dev Drive location: {location.Path}");
                Location = location.Path;
            }
            else
            {
                _log.Information("No location selected for Dev Drive");
            }
        }
        catch (Exception e)
        {
            _log.Error(e, "Failed to open folder picker.");
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
            _log.Information("Closing dev drive window");
            DevDriveWindowContainer.Close();
        }
    }

    /// <summary>
    /// Updates the associated Dev Drive and property fields for the UI with new details from another
    /// Dev Drive.
    /// </summary>
    public void UpdateDevDriveInfo(IDevDrive devDrive)
    {
        _log.Information("Updating Dev Drive info");
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
        DriveLetters.Clear();
        DriveLetters.AddRange(_devDriveManager.GetAvailableDriveLetters(AssociatedDrive.DriveLetter));
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
    public async Task LaunchDisksAndVolumesSettingsPageAsync()
    {
        // Critical level approved by subhasan
        TelemetryFactory.Get<ITelemetry>().Log(
            "LaunchDisksAndVolumesSettingsPageTriggered",
            LogLevel.Critical,
            new DisksAndVolumesSettingsPageTriggeredEvent(source: "DevDriveView"),
            _host.GetService<SetupFlowOrchestrator>().ActivityId);
        await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
    }

    /// <summary>
    /// Save button click command sends the <see cref="Models.DevDrive"/> associated with
    /// with the view model to the  <see cref="DevDriveManager"/> to have its contents validated
    /// only when the validation is successful do we close the window when the save button is clicked.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveButton()
    {
        _log.Information("Saving changes to Dev Drive");
        ByteUnit driveUnitOfMeasure = (ByteUnit)ComboBoxByteUnit;
        var tempDrive = new DevDrive()
        {
            DriveLetter = ComboBoxDriveLetter.Value,
            DriveSizeInBytes = DevDriveUtil.ConvertToBytes(Size, driveUnitOfMeasure),
            DriveUnitOfMeasure = driveUnitOfMeasure,
            DriveLocation = Location,
            DriveLabel = DriveLabel,
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
    /// Only allow users to save when there are no errors in the UI.
    /// </summary>
    /// <returns>Boolean where true will enable the save button and sale disables the button.</returns>
    private bool CanSave()
    {
        return !(FolderLocationError.HasValue || DriveLetterError.HasValue || FileNameAndSizeErrorList.Any());
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
        _log.Information("Launching window to set up Dev Drive");
        ResetErrors();
        DevDriveWindowContainer = new(this);
        DevDriveWindowContainer.Closed += ViewContainerClosed;

        // Setting this before the window activates prevents the window from showing up on the screen,
        // then moving abruptly to the center.
        DevDriveWindowContainer.CenterOnWindow();
        DevDriveWindowContainer.Activate();
        IsDevDriveWindowOpen = true;
        RefreshDriveLetterToSizeMapping();
        DriveLetters.Clear();
        DriveLetters.AddRange(_devDriveManager.GetAvailableDriveLetters(AssociatedDrive.DriveLetter));

        // If state is invalid then show errors in the UI as soon as we launch the window.
        if (_concreteDevDrive.State != DevDriveState.New ||
            !ComboBoxDriveLetter.HasValue ||
            !DriveLetters.Contains(ComboBoxDriveLetter.Value))
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
    /// Shows the user all errors found after clicking the save button. The set of errors come from the
    /// DevDriveManager, and this method matches the errors with the relevant UI element.
    /// </summary>
    public void ShowErrorInUI(ISet<DevDriveValidationResult> resultSet)
    {
        var tempfileNameAndSizeErrorList = new List<DevDriveValidationResult>();
        DriveLetterError = null;
        FolderLocationError = null;
        foreach (DevDriveValidationResult result in resultSet)
        {
            _log.Error($"Input validation Error in Dev Drive window: {result}");
            switch (result)
            {
                case DevDriveValidationResult.NoDriveLettersAvailable:
                case DevDriveValidationResult.DriveLetterNotAvailable:
                    DriveLetterError ??= result;
                    break;
                case DevDriveValidationResult.InvalidFolderLocation:
                    FolderLocationError = result;
                    break;
                case DevDriveValidationResult.InvalidDriveLabel:
                case DevDriveValidationResult.NotEnoughFreeSpace:
                case DevDriveValidationResult.FileNameAlreadyExists:
                    tempfileNameAndSizeErrorList.Add(result);
                    break;
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
        DriveLetterError = null;
        FolderLocationError = null;
        FileNameAndSizeErrorList.Clear();
    }

    /// <summary>
    /// Refreshes the mapping between Drive letters currently in use on the users machines and the total free space
    /// available on them.
    /// </summary>
    private void RefreshDriveLetterToSizeMapping()
    {
        try
        {
            // Calling the TotalFreeSpace property when the drive isn't ready will throw an exception, make its total available space set to 0.
            // This way it cannot be used to create a Dev Drive. The GetDrives method only returns drives that have drive letters. The name property returns the
            // drive letter in the form of "DriveLetter:\". E.g C:\
            DriveLetterToSizeMapping = DriveInfo.GetDrives().ToDictionary(drive => drive.Name.FirstOrDefault(), drive => drive.IsReady ? (ulong)drive.TotalFreeSpace : 0);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to refresh the drive letter to size mapping.");

            // Clear the mapping since it can't be refreshed. This shouldn't happen unless DriveInfo.GetDrives() fails. In that case we won't know which drive
            // in the list is causing GetDrives()'s to throw. If there are values inside the dictionary at this point, they could be stale. Clearing the list
            // allows users to at least attempt to use the location they want to create the virtual disk in. Ultimately if the location is really unavailable the virtual disk
            // won't be created and we will send an error to the UI in the loading page.
            DriveLetterToSizeMapping.Clear();
        }
    }

    /// <summary>
    /// Validates the text input in the Dev Drive name textbox when the text changes. Show an error infobar when the text is invalid.
    /// </summary>
    /// <remarks>
    /// There are currently only 2 invalid states
    /// 1.When the text has invalid characters. <see cref="DevDriveUtil.IsInvalidFileNameOrPath="/>
    /// 2.When the file name already exists in the given location.
    /// </remarks
    private void ValidateDriveLabel()
    {
        FileNameAndSizeErrorList.Remove(DevDriveValidationResult.InvalidDriveLabel);
        if (string.IsNullOrEmpty(DriveLabel) || DevDriveUtil.IsInvalidFileNameOrPath(InvalidCharactersKind.FileName, DriveLabel))
        {
            FileNameAndSizeErrorList.Add(DevDriveValidationResult.InvalidDriveLabel);
        }

        FileNameAndSizeErrorList.Remove(DevDriveValidationResult.FileNameAlreadyExists);
        if (!string.IsNullOrEmpty(Location) && File.Exists(Path.Combine(Location, DriveLabel + ".vhdx")))
        {
            FileNameAndSizeErrorList.Add(DevDriveValidationResult.FileNameAlreadyExists);
        }
    }

    /// <summary>
    /// Validates the numeric input value in the size numberbox. Show an error infobar when the value is invalid.
    /// </summary>
    /// <remarks>
    /// There is currently only 1 invalid state
    /// 1.When numeric value given is greater than the total available free space on the Drive the user intends to create the virtual disk file on.
    /// </remarks
    private void ValidateDriveSize()
    {
        FileNameAndSizeErrorList.Remove(DevDriveValidationResult.NotEnoughFreeSpace);
        var lengthAfterTrim = string.IsNullOrEmpty(Location) ? string.Empty : Location.Trim();

        // If newValue is not a number, show the error infobar.
        var shouldShowSizeError = !double.IsFinite(Size);
        if (!shouldShowSizeError && lengthAfterTrim.Length > 0)
        {
            // Refresh the mapping because when the user frees up drive space, totalAvailableSpace needs to be updated for the drive they plan
            // on using.
            RefreshDriveLetterToSizeMapping();
            ulong totalAvailableSpace;
            if (DriveLetterToSizeMapping.TryGetValue(char.ToUpperInvariant(lengthAfterTrim[0]), out totalAvailableSpace))
            {
                shouldShowSizeError = DevDriveUtil.ConvertToBytes(Size, (ByteUnit)ComboBoxByteUnit) > totalAvailableSpace;
            }
        }

        if (shouldShowSizeError)
        {
            FileNameAndSizeErrorList.Add(DevDriveValidationResult.NotEnoughFreeSpace);
        }
    }

    /// <summary>
    /// Called when there is a TextChanged event from the Dev Drive location textbox. Show an error infobar when the text is invalid.
    /// </summary>
    /// <remarks>
    /// There are currently 5 invalid states
    /// 1. When the location length is less than 3. We expect the location to be in the form of drive letter, followed by a colon followed by a slash. E.g C:\
    /// 2. Path is a network path.
    /// 3. When the path root to the location does not exist. E.g user attempting to use a drive that does not exist.
    /// 4. When the location is not fully qualified.
    /// 5. When the location has invalid characters. <see cref="DevDriveUtil.IsInvalidFileNameOrPath="/>
    /// </remarks>
    /// <param name="location">The folder path the user will use to create virtual disk in</param>
    private void ValidateDriveLocation()
    {
        FolderLocationError = null;

        if (Location.Length < 3 ||
            IsNetworkPath(Location) ||
            !Directory.Exists(Path.GetPathRoot(Location)) ||
            !Path.IsPathFullyQualified(Location) ||
            DevDriveUtil.IsInvalidFileNameOrPath(InvalidCharactersKind.Path, Location))
        {
            FolderLocationError = DevDriveValidationResult.InvalidFolderLocation;
        }
        else
        {
            // Location changed, so the size may now be too large for this location or could now be the right size
            // after being too small.
            ValidateDriveSize();
        }
    }

    /// <summary>
    /// Checks whether the path is a network path.
    /// </summary>
    /// <param name="path">The path that could possibly be a network path</param>
    /// <returns>Boolean where true means the path is a network path and false otherwise.</returns>
    private bool IsNetworkPath(string path)
    {
        unsafe
        {
            fixed (char* tempPath = path)
            {
                return PInvoke.PathIsNetworkPath(tempPath).Equals(new BOOL(true));
            }
        }
    }

    /// <summary>
    /// Validates the current selected drive letter and shows an error info in the UI, if an error is found.
    /// </summary>
    private void ValidateDriveLetter()
    {
        DriveLetterError = null;

        if (DriveLetters.Count == 0)
        {
            DriveLetterError = DevDriveValidationResult.NoDriveLettersAvailable;
        }
        else if (!ComboBoxDriveLetter.HasValue || !DriveLetters.Contains(ComboBoxDriveLetter.Value))
        {
            DriveLetterError = DevDriveValidationResult.DriveLetterNotAvailable;
        }
    }

    /// <summary>
    /// Validation event handler for when a property changes.
    /// </summary>
    /// <param name="propertyName">property name of the property that will be validated</param>
    private void ValidatePropertyByName(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(DriveLabel):
                ValidateDriveLabel();
                break;
            case nameof(Location):
                ValidateDriveLocation();
                break;
            case nameof(Size):
                ValidateDriveSize();
                break;
            case nameof(ComboBoxDriveLetter):
                ValidateDriveLetter();
                break;
        }
    }
}
