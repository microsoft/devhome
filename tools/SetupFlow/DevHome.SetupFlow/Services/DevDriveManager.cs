// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Helpers;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Ioctl;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Class for Dev Drive manager. The Dev Drive manager is the mediator between the Dev Drive view Model for
/// the Dev Drive window and the objects that requested the window to be launched. Message passing between the two so they do not
/// need to know of eachothers existence. The Dev Drive manager uses a set to keep track of the Dev Drives created by the user.
/// </summary>
public class DevDriveManager : IDevDriveManager
{
    private readonly IHost _host;
    private readonly string _defaultVhdxLocation;
    private readonly string _defaultVhdxName;
    private readonly ISetupFlowStringResource _stringResource;

    // Query flag for persistent state info of the volume, the presense of this flag will let us know
    // its a Dev drive. TODO: Update this once in Windows SDK
    private readonly uint _devDriveVolumeStateFlag = 0x00002000;

    /// <summary>
    /// Set that holds Dev Drives that have been created through the Dev Drive manager.
    /// </summary>
    private readonly HashSet<IDevDrive> _devDrives = new ();

    private DevDriveViewModel _devDriveViewModel;

    /// <summary>
    /// Gets or sets the previous Dev Drive object that the user saved.
    /// </summary>
    /// <remarks>
    /// Used only when the Repo tool cancels their dialog even after selecting save from the Dev Drive Window.
    /// </remarks>
    public IDevDrive PreviousDevDrive
    {
        get; set;
    }

    /// <inheritdoc/>
    public int RepositoriesUsingDevDrive
    {
        get; private set;
    }

    /// <inheritdoc/>
    public IList<IDevDrive> DevDrivesMarkedForCreation => _devDrives.ToList();

    /// <summary>
    /// Gets a view model that will show information related to a Dev Drive we create
    /// </summary>
    public DevDriveViewModel ViewModel
    {
        get
        {
            _devDriveViewModel ??= _host.GetService<DevDriveViewModel>();
            return _devDriveViewModel;
        }
    }

    /// <summary>
    /// Event that requesters can subscribe to, to know when a <see cref="DevDriveWindow"/> has closed.
    /// </summary>
    public event EventHandler<IDevDrive> ViewModelWindowClosed = (sender, e) => { };

    /// <summary>
    /// Event that view model can subscribe to, to know if a requester wants them to close their <see cref="DevDriveWindow"/>.
    /// </summary>
    public event EventHandler<IDevDrive> RequestToCloseViewModelWindow = (sender, e) => { };

    public DevDriveManager(IHost host, ISetupFlowStringResource stringResource)
    {
        _host = host;
        _stringResource = stringResource;
        _defaultVhdxLocation = stringResource.GetLocalized(StringResourceKey.DevDriveDefaultFolderName);
        _defaultVhdxName = stringResource.GetLocalized(StringResourceKey.DevDriveDefaultFileName);
    }

    /// <inheritdoc/>
    public Task<bool> LaunchDevDriveWindow(IDevDrive devDrive)
    {
        // Only allow one Dev Drive window to be opened at a time.
        if (ViewModel.IsDevDriveWindowOpen)
        {
            return Task.FromResult(false);
        }

        ViewModel.UpdateDevDriveInfo(devDrive);
        return ViewModel.LaunchDevDriveWindow();
    }

    /// <inheritdoc/>
    public void NotifyDevDriveWindowClosed(IDevDrive newDevDrive)
    {
        PreviousDevDrive = _devDrives.First();
        _devDrives.Clear();
        _devDrives.Add(newDevDrive);
        ViewModelWindowClosed(null, newDevDrive);
    }

    /// <inheritdoc/>
    public void RequestToCloseDevDriveWindow(IDevDrive devDrive)
    {
        RequestToCloseViewModelWindow(null, devDrive);
    }

    /// <summary>
    /// Creates a new dev drive object. This creates a  <see cref="IDevDrive"/> object with pre-populated data. The size,
    /// name, folder location for vhdx file and drive letter will be prepopulated.
    /// </summary>
    /// <returns>
    /// An Dev Drive thats associated with a viewmodel and a result that indicates whether the operation
    /// was successful.
    /// </returns>
    public (DevDriveValidationResult, IDevDrive) GetNewDevDrive()
    {
        // Currently we only support creating one Dev Drive at a time. If one was
        // produced before reuse it.
        if (_devDrives.Any())
        {
            return (DevDriveValidationResult.Successful, _devDrives.First());
        }

        var devDrive = new Models.DevDrive();
        var result = UpdateDevDriveWithDefaultInfo(ref devDrive);
        if (result == DevDriveValidationResult.Successful)
        {
            var taskGroups = _host.GetService<SetupFlowOrchestrator>().TaskGroups;
            var group = taskGroups.Single(x => x.GetType() == typeof(DevDriveTaskGroup));
            if (group is DevDriveTaskGroup driveTaskGroup)
            {
                ViewModel.TaskGroup = driveTaskGroup;
            }

            ViewModel.UpdateDevDriveInfo(devDrive);
            _devDrives.Add(devDrive);
        }

        return (result, devDrive);
    }

    /// <inheritdoc/>
    public IEnumerable<IDevDrive> GetAllDevDrivesThatExistOnSystem()
    {
        try
        {
            var devDrives = new List<IDevDrive>();
            ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\Microsoft\\Windows\\Storage", "SELECT * FROM MSFT_Volume");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                var volumePath = queryObj["Path"] as string;
                var volumeLabel = queryObj["FileSystemLabel"] as string;
                var volumeSize = queryObj["Size"];
                var volumeLetter = queryObj["DriveLetter"];
                uint outputSize;
                var volumeInfo = new FILE_FS_PERSISTENT_VOLUME_INFORMATION { };
                var inputVolumeInfo = new FILE_FS_PERSISTENT_VOLUME_INFORMATION { };
                inputVolumeInfo.FlagMask = _devDriveVolumeStateFlag;
                inputVolumeInfo.Version = 1;

                SafeFileHandle volumeFileHandle = PInvoke.CreateFile(
                    volumePath,
                    FILE_ACCESS_FLAGS.FILE_READ_ATTRIBUTES | FILE_ACCESS_FLAGS.FILE_WRITE_ATTRIBUTES,
                    FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                    null,
                    FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                    FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
                    null);

                if (volumeFileHandle.IsInvalid)
                {
                    continue;
                }

                unsafe
                {
                    var result = PInvoke.DeviceIoControl(
                        volumeFileHandle,
                        PInvoke.FSCTL_QUERY_PERSISTENT_VOLUME_STATE,
                        &inputVolumeInfo,
                        (uint)sizeof(FILE_FS_PERSISTENT_VOLUME_INFORMATION),
                        &volumeInfo,
                        (uint)sizeof(FILE_FS_PERSISTENT_VOLUME_INFORMATION),
                        &outputSize,
                        null);

                    if (!result)
                    {
                        continue;
                    }

                    if ((volumeInfo.VolumeFlags & _devDriveVolumeStateFlag) > 0 &&
                        volumeLetter is char newLetter && volumeSize is ulong newSize)
                    {
                        var isInTerabytes = newSize >= DevDriveUtil.OneTbInBytes;
                        var newDevDrive = new Models.DevDrive
                        {
                            DriveLetter = newLetter,
                            DriveSizeInBytes = newSize,
                            DriveUnitOfMeasure = isInTerabytes ? ByteUnit.TB : ByteUnit.GB,
                            DriveLocation = string.Empty,
                            DriveLabel = volumeLabel,
                            State = DevDriveState.ExistsOnSystem,
                        };

                        devDrives.Add(newDevDrive);
                    }
                }
            }

            return devDrives;
        }
        catch (Exception ex)
        {
            // Log then return empty list, as this only means we don't show the user their existing dev drive. Not catastrophic failure.
            Log.Logger?.ReportError(nameof(DevDriveManager), $"Failed Get existing Dev Drives. ErrorCode: {ex.HResult}, Msg: {ex.Message}");
            return new List<IDevDrive>();
        }
    }

    /// <summary>
    /// Gets prepoppulated data and updates the passed in dev drive object with it.
    /// </summary>
    /// <returns>
    /// A result that indicates whether the operation was successful.
    /// </returns>
    private DevDriveValidationResult UpdateDevDriveWithDefaultInfo(ref Models.DevDrive devDrive)
    {
        try
        {
            var location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.GetPathRoot(Environment.SystemDirectory);
            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(root))
            {
                return DevDriveValidationResult.DefaultFolderNotAvailable;
            }

            var drive = new DriveInfo(root);
            if (DevDriveUtil.MinDevDriveSizeInBytes > (ulong)drive.AvailableFreeSpace)
            {
                return DevDriveValidationResult.NotEnoughFreeSpace;
            }

            var availableLetters = GetAvailableDriveLetters();
            if (!availableLetters.Any())
            {
                return DevDriveValidationResult.NoDriveLettersAvailable;
            }

            devDrive.DriveLetter = availableLetters[0];
            devDrive.DriveSizeInBytes = DevDriveUtil.MinDevDriveSizeInBytes;
            devDrive.DriveUnitOfMeasure = ByteUnit.GB;
            devDrive.DriveLocation = location;
            uint count = 1;
            var fullPath = Path.Combine(location, $"{_defaultVhdxName}.vhdx");
            var fileName = _defaultVhdxName;

            // If original default file name exists we'll increase the number next to the filename
            while (File.Exists(fullPath) && count <= 1000)
            {
                fileName = $"{_defaultVhdxName} {count}";
                fullPath = Path.Combine(location, $"{fileName}.vhdx");
                count++;
            }

            devDrive.DriveLabel = fileName;
            devDrive.State = DevDriveState.New;

            return DevDriveValidationResult.Successful;
        }
        catch (Exception ex)
        {
            // we don't need to rethrow the exception/crash, we need to tell the user we couldn't find the default folder.
            Log.Logger?.ReportError(nameof(DevDriveManager), $"Failed Get default folder for Dev Drive. {ex.Message}");
            return DevDriveValidationResult.DefaultFolderNotAvailable;
        }
    }

    /// <inheritdoc/>
    public ISet<DevDriveValidationResult> GetDevDriveValidationResults(IDevDrive devDrive)
    {
        var returnSet = new HashSet<DevDriveValidationResult>();
        var minValue = DevDriveUtil.ConvertToBytes(DevDriveUtil.MinSizeForGbComboBox, ByteUnit.GB);
        var maxValue = DevDriveUtil.ConvertToBytes(DevDriveUtil.MaxSizeForTbComboBox, ByteUnit.TB);

        if (devDrive == null)
        {
            returnSet.Add(DevDriveValidationResult.ObjectWasNull);
            return returnSet;
        }

        if (minValue > devDrive.DriveSizeInBytes || devDrive.DriveSizeInBytes > maxValue)
        {
            returnSet.Add(DevDriveValidationResult.InvalidDriveSize);
        }

        if (string.IsNullOrEmpty(devDrive.DriveLabel) ||
            devDrive.DriveLabel.Length > DevDriveUtil.MaxDriveLabelSize ||
            DevDriveUtil.IsInvalidFileNameOrPath(InvalidCharactersKind.FileName, devDrive.DriveLabel))
        {
            returnSet.Add(DevDriveValidationResult.InvalidDriveLabel);
        }

        // Only check if the drive letter isn't already being used by another Dev Drive object in memory
        // if we're not in the process of creating it on the System.
        if (_devDrives.FirstOrDefault(drive => drive.DriveLetter == devDrive.DriveLetter && drive.ID != devDrive.ID) != null)
        {
            returnSet.Add(DevDriveValidationResult.DriveLetterNotAvailable);
        }

        var result = IsPathValid(devDrive);
        if (result != DevDriveValidationResult.Successful)
        {
            returnSet.Add(result);
        }

        var driveLetterSet = new HashSet<char>();
        foreach (var curDriveOnSystem in DriveInfo.GetDrives())
        {
            driveLetterSet.Add(curDriveOnSystem.Name[0]);
            if (driveLetterSet.Contains(devDrive.DriveLetter))
            {
                returnSet.Add(DevDriveValidationResult.DriveLetterNotAvailable);
            }

            // If drive location is invalid, we would have already captured this in the IsPathValid call above.
            var potentialRoot = string.IsNullOrEmpty(devDrive.DriveLocation) ? '\0' : devDrive.DriveLocation[0];
            if (potentialRoot == curDriveOnSystem.Name[0] &&
                (devDrive.DriveSizeInBytes > (ulong)curDriveOnSystem.TotalFreeSpace))
            {
                returnSet.Add(DevDriveValidationResult.NotEnoughFreeSpace);
            }
        }

        if (returnSet.Count == 0)
        {
            returnSet.Add(DevDriveValidationResult.Successful);
        }

        return returnSet;
    }

    /// <inheritdoc/>
    public IList<char> GetAvailableDriveLetters(char? usedLetterToKeepInList = null)
    {
        var driveLetterSet = new SortedSet<char>(DevDriveUtil.DriveLetterCharArray);
        foreach (var drive in DriveInfo.GetDrives())
        {
            driveLetterSet.Remove(drive.Name[0]);
        }

        foreach (var devDrive in _devDrives)
        {
            if (usedLetterToKeepInList == null || usedLetterToKeepInList != devDrive.DriveLetter)
            {
                driveLetterSet.Remove(devDrive.DriveLetter);
            }
        }

        return driveLetterSet.ToList();
    }

    /// <summary>
    /// Consolidated logic that checks if a Dev Drive location and combined path with Dev Drive label is valid.
    /// The location has to exist on the system if it is not a network path.
    /// </summary>
    /// <param name="devDrive"> The IDevDrive object to be validated</param>
    /// <returns>Bool where true means the location is valid and false if invalid</returns>
    private DevDriveValidationResult IsPathValid(IDevDrive devDrive)
    {
        if (string.IsNullOrEmpty(devDrive.DriveLocation) ||
            devDrive.DriveLocation.Length > DevDriveUtil.MaxDrivePathLength ||
            DevDriveUtil.IsInvalidFileNameOrPath(InvalidCharactersKind.Path, devDrive.DriveLocation))
        {
            return DevDriveValidationResult.InvalidFolderLocation;
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

                if (!isNetworkPath)
                {
                    if (!Directory.Exists(fileInfo.FullName))
                    {
                        return DevDriveValidationResult.InvalidFolderLocation;
                    }

                    if (File.Exists(Path.Combine(fileInfo.FullName, devDrive.DriveLabel + ".vhdx")))
                    {
                        return DevDriveValidationResult.FileNameAlreadyExists;
                    }
                }
            }
            else
            {
                return DevDriveValidationResult.InvalidFolderLocation;
            }
        }
        catch (Exception)
        {
            return DevDriveValidationResult.InvalidFolderLocation;
        }

        return DevDriveValidationResult.Successful;
    }

    /// <inheritdoc/>
    public void RemoveAllDevDrives()
    {
        _devDrives.Clear();
        ViewModel.RemoveTasks();
        RepositoriesUsingDevDrive = 0;
    }

    /// <inheritdoc/>
    public void CancelChangesToDevDrive()
    {
        if (PreviousDevDrive != null)
        {
            _devDrives.Clear();
            _devDrives.Add(PreviousDevDrive);
        }
    }

    /// <inheritdoc/>
    public void ConfirmChangesToDevDrive()
    {
        if (_devDrives.Any())
        {
            PreviousDevDrive = _devDrives.First();
        }
    }

    /// <inheritdoc/>
    public void IncreaseRepositoriesCount(int count)
    {
        RepositoriesUsingDevDrive += count;
    }

    /// <inheritdoc/>
    public void DecreaseRepositoriesCount()
    {
        if (RepositoriesUsingDevDrive > 0)
        {
            RepositoriesUsingDevDrive--;
            if (RepositoriesUsingDevDrive == 0)
            {
                _devDrives.Clear();
                PreviousDevDrive = null;
                ViewModel.RemoveTasks();
            }
        }
    }
}
