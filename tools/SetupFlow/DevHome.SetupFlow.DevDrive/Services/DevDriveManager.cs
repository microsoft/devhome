// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Utilities;
using DevHome.SetupFlow.DevDrive.ViewModels;
using DevHome.SetupFlow.DevDrive.Windows;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.DevDrive.Services;

/// <summary>
/// Class for Dev Drive manager. The Dev Drive manager is the mediator between objects that will be a viewmodel for
/// a Dev Drive window and the objects that requested the window to be launched. Message passing between the two so they do not
/// need to know of eachothers existence. The Dev Drive manager uses a Dictionary object to keep the association between
/// the Dev Drive and the Dev Drive window.
/// </summary>
public class DevDriveManager : IDevDriveManager
{
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly IDevDriveStorageOperator _devDriveStorageOperator = new DevDriveStorageOperator();
    private readonly string _defaultVhdxLocation;
    private readonly string _defaultVhdxName;
    private readonly IStringResource _stringResource;

    /// <summary>
    /// Dictionary that Associates a Dev Drive object with a view model.
    /// </summary>
    private readonly Dictionary<IDevDrive, IDevDriveWindowViewModel> _devDriveToViewModelMap = new ();

    /// <inheritdoc/>
    public IList<IDevDrive> DevDrivesMarkedForCreation => _devDriveToViewModelMap.Keys.Where(devDrive => devDrive.State == DevDriveState.New).ToList();

    /// <summary>
    /// Event that requesters can subscribe to, to know when a <see cref="DevDriveWindow"/> has closed.
    /// </summary>
    public event EventHandler<IDevDrive> ViewModelWindowClosed = (sender, e) => { };

    /// <summary>
    /// Event that view model can subscribe to, to know if a requester wants them to close their <see cref="DevDriveWindow"/>.
    /// </summary>
    public event EventHandler<IDevDrive> RequestToCloseViewModelWindow = (sender, e) => { };

    public DevDriveManager(IHost host, ILogger logger, IStringResource stringResource)
    {
        _host = host;
        _logger = logger;
        _stringResource = stringResource;
        _defaultVhdxLocation = stringResource.GetLocalized(StringResourceKey.DevDriveDefaultFolderName);
        _defaultVhdxName = stringResource.GetLocalized(StringResourceKey.DevDriveDefaultFileName);
    }

    /// <inheritdoc/>
    public Task<DevDriveOperationResult> CreateDevDrive(IDevDrive devDrive) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<bool> LaunchDevDriveWindow(IDevDrive devDrive)
    {
        try
        {
            IDevDriveWindowViewModel viewModel;
            if (_devDriveToViewModelMap.TryGetValue(devDrive, out viewModel))
            {
                return _devDriveToViewModelMap[devDrive].LaunchDevDriveWindow();
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to launch a new Dev Drive window. {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public void NotifyDevDriveWindowClosed(IDevDrive devDrive)
    {
        ViewModelWindowClosed(null, devDrive);
    }

    /// <inheritdoc/>
    public void RequestToCloseDevDriveWindow(IDevDrive devDrive)
    {
        RequestToCloseViewModelWindow(null, devDrive);
        RemoveDevDrive(devDrive);
    }

    /// <summary>
    /// Creates a new dev drive object. This creates a  <see cref="IDevDrive"/> object with pre-populated data. The size,
    /// name, folder location for vhdx file and drive letter will be prepopulated.
    /// </summary>
    /// <returns>
    /// An Dev Drive thats associated with a viewmodel and a result that indicates whether the operation
    /// was successful.
    /// </returns>
    public (DevDriveOperationResult, IDevDrive) GetNewDevDrive()
    {
        var devDrive = new Models.DevDrive();
        var result = UpdateDevDriveWithDefaultInfo(ref devDrive);
        if (result != DevDriveOperationResult.Successful)
        {
            return (result, devDrive);
        }

        var newViewModel = _host.CreateInstance<DevDriveViewModel>(devDrive);
        _devDriveToViewModelMap[newViewModel.AssociatedDrive] = newViewModel;
        return (result, devDrive);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IDevDrive>> GetAllDevDrivesThatExistOnSystem()
    {
        return Task.Run(() =>
        {
            // TODO: Return empty list so code does not throw.
            // SHould fix with implementation.
            IEnumerable<IDevDrive> toReturn = new List<IDevDrive>();
            return toReturn;
        });
    }

    /// <summary>
    /// Gets prepoppulated data and updates the passed in dev drive object with it.
    /// </summary>
    /// <returns>
    /// A result that indicates whether the operation was successful.
    /// </returns>
    private DevDriveOperationResult UpdateDevDriveWithDefaultInfo(ref Models.DevDrive devDrive)
    {
        try
        {
            var location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.GetPathRoot(Environment.SystemDirectory);
            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(root))
            {
                return DevDriveOperationResult.DefaultFolderNotAvailable;
            }

            var drive = new DriveInfo(root);
            if (DevDriveUtil.MinDevDriveSizeInBytes > (ulong)drive.AvailableFreeSpace)
            {
                return DevDriveOperationResult.NotEnoughFreeSpace;
            }

            var availableLetters = GetAvailableDriveLetters(null);
            if (!availableLetters.Any())
            {
                return DevDriveOperationResult.NoDriveLettersAvailable;
            }

            devDrive.DriveLetter = availableLetters[0];
            devDrive.DriveSizeInBytes = DevDriveUtil.MinDevDriveSizeInBytes;
            devDrive.DriveUnitOfMeasure = ByteUnit.GB;
            devDrive.DriveLocation = location;
            devDrive.DriveLabel = _defaultVhdxName;
            devDrive.State = DevDriveState.New;

            return DevDriveOperationResult.Successful;
        }
        catch (Exception ex)
        {
            // we don't need to keep the exception/crash, we need to tell the user we couldn't find the appdata
            // folder.
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed Get default folder for Dev Drive. {ex.Message}");
            return DevDriveOperationResult.DefaultFolderNotAvailable;
        }
    }

    /// <inheritdoc/>
    public ISet<DevDriveOperationResult> GetDevDriveValidationResults(IDevDrive devDrive)
    {
        var returnSet = new HashSet<DevDriveOperationResult>();
        var minValue = DevDriveUtil.ConvertToBytes(DevDriveUtil.MinSizeForGbComboBox, ByteUnit.GB);
        var maxValue = DevDriveUtil.ConvertToBytes(DevDriveUtil.MaxSizeForTbComboBox, ByteUnit.TB);

        if (devDrive == null)
        {
            returnSet.Add(DevDriveOperationResult.ObjectWasNull);
            return returnSet;
        }

        if (minValue > devDrive.DriveSizeInBytes || devDrive.DriveSizeInBytes > maxValue)
        {
            returnSet.Add(DevDriveOperationResult.InvalidDriveSize);
        }

        if (string.IsNullOrEmpty(devDrive.DriveLabel) ||
            devDrive.DriveLabel.Length > DevDriveUtil.MaxDriveLabelSize ||
            DevDriveUtil.IsInvalidFileNameOrPath(InvalidCharactersKind.FileName, devDrive.DriveLabel))
        {
            returnSet.Add(DevDriveOperationResult.InvalidDriveLabel);
        }

        // Only check if the drive letter isn't already being used by another Dev Drive object in memory
        // if we're not in the process of creating it on the System.
        if (devDrive.State != DevDriveState.Creating &&
            _devDriveToViewModelMap.Keys.Where(drive => drive.DriveLetter == devDrive.DriveLetter && drive.ID != devDrive.ID).Any())
        {
            returnSet.Add(DevDriveOperationResult.DriveLetterNotAvailable);
        }

        if (!IsFolderLocationValid(devDrive))
        {
            returnSet.Add(DevDriveOperationResult.InvalidFolderLocation);
        }

        var driveLetterSet = new HashSet<char>();
        foreach (var curDriveOnSystem in DriveInfo.GetDrives())
        {
            driveLetterSet.Add(curDriveOnSystem.Name[0]);
            if (driveLetterSet.Contains(devDrive.DriveLetter))
            {
                returnSet.Add(DevDriveOperationResult.DriveLetterNotAvailable);
            }

            // If drive location is invalid, we would have already captured this in the IsFolderLocationValid call above.
            var potentialRoot = string.IsNullOrEmpty(devDrive.DriveLocation) ? '\0' : devDrive.DriveLocation[0];
            if (potentialRoot == curDriveOnSystem.Name[0] &&
                (devDrive.DriveSizeInBytes > (ulong)curDriveOnSystem.TotalFreeSpace))
            {
                returnSet.Add(DevDriveOperationResult.NotEnoughFreeSpace);
            }
        }

        if (returnSet.Count == 0)
        {
            returnSet.Add(DevDriveOperationResult.Successful);
        }

        return returnSet;
    }

    /// <inheritdoc/>
    public IList<char> GetAvailableDriveLetters(IDevDrive devDrive)
    {
        var driveLetterSet = new SortedSet<char>(DevDriveUtil.DriveLetterCharArray);
        foreach (var drive in DriveInfo.GetDrives())
        {
            driveLetterSet.Remove(drive.Name[0]);
        }

        var driveLetter = devDrive?.DriveLetter;
        foreach (var drive in _devDriveToViewModelMap.Keys)
        {
            if (driveLetter == null || driveLetter != drive.DriveLetter)
            {
                driveLetterSet.Remove(drive.DriveLetter);
            }
        }

        return driveLetterSet.ToList();
    }

    /// <inheritdoc/>
    public DevDriveOperationResult RemoveDevDrive(IDevDrive devDrive)
    {
        if (_devDriveToViewModelMap.ContainsKey(devDrive))
        {
            _devDriveToViewModelMap.Remove(devDrive);
            return DevDriveOperationResult.Successful;
        }

        return DevDriveOperationResult.DevDriveNotFound;
    }

    /// <summary>
    /// Consolidated logic that checks if a Dev Drive location string is valid. The location
    /// has to exist on the system if it is not a network path.
    /// </summary>
    /// <param name="devDrive"> The IDevDrive object to be validated</param>
    /// <returns>Bool where true means the location is valid and false if invalid</returns>
    private bool IsFolderLocationValid(IDevDrive devDrive)
    {
        if (string.IsNullOrEmpty(devDrive.DriveLocation) ||
            devDrive.DriveLocation.Length > DevDriveUtil.MaxDrivePathLength ||
            DevDriveUtil.IsInvalidFileNameOrPath(InvalidCharactersKind.Path, devDrive.DriveLocation))
        {
            return false;
        }

        string locationRoot;
        try
        {
            var fileInfo = new FileInfo(devDrive.DriveLocation);
            locationRoot = Path.GetPathRoot(fileInfo.FullName);
            if (!string.IsNullOrEmpty(locationRoot))
            {
                var path = fileInfo.FullName.ToString();
                var isNetworkPath = false;
                unsafe
                {
                    fixed (char* tempPath = path)
                    {
                        isNetworkPath = PInvoke.PathIsNetworkPath(tempPath).Equals(new BOOL(true));
                    }
                }

                if (!isNetworkPath && !Directory.Exists(fileInfo.FullName))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}
