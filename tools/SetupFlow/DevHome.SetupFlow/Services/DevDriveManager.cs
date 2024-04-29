// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Management.Infrastructure;
using Microsoft.Win32.SafeHandles;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Ioctl;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Class for Dev Drive manager. The Dev Drive manager is the mediator between the Dev Drive view Model for
/// the Dev Drive window and the objects that requested the window to be launched. Message passing between the two so they do not
/// need to know of each others existence. The Dev Drive manager uses a set to keep track of the Dev Drives created by the user.
/// </summary>
public class DevDriveManager : IDevDriveManager
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevDriveManager));
    private readonly IHost _host;
    private readonly string _defaultVhdxFolderName;
    private readonly string _defaultVhdxName;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly string _defaultDevDriveLocation;
    private const uint MaxNumberToAppendToFileName = 1000;

    // Query flag for persistent state info of the volume, the presence of this flag will let us know
    // its a Dev drive. TODO: Update this once in Windows SDK
    // https://github.com/microsoft/devhome/issues/634
    private readonly uint _devDriveVolumeStateFlag = 0x00002000;

    // Query flag for persistent state info of the volume, the presence of this flag will let us know
    // its a trusted Dev drive.
    private readonly uint _devDriveVolumeStateTrustedFlag = 0x00004000;

    /// <summary>
    /// Set that holds Dev Drives that have been created through the Dev Drive manager.
    /// </summary>
    private readonly HashSet<IDevDrive> _devDrives = new();

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

    /// <summary>
    /// Gets the default location we use to save a Dev Drive to. Requires admin rights, to create, if not already created.
    /// </summary>
    public string DefaultDevDriveLocation => _defaultDevDriveLocation;

    /// <inheritdoc/>
    public IList<IDevDrive> DevDrivesMarkedForCreation => RepositoriesUsingDevDrive > 0 ? _devDrives.ToList() : new List<IDevDrive>();

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
        _defaultVhdxFolderName = stringResource.GetLocalized(StringResourceKey.DevDriveDefaultFolderName);
        _defaultVhdxName = stringResource.GetLocalized(StringResourceKey.DevDriveDefaultFileName);
        var location = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _defaultDevDriveLocation = Path.Combine(location, _defaultVhdxFolderName);
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
    /// The Dev Drive to be created
    /// </returns>
    public IDevDrive GetNewDevDrive()
    {
        // Currently only one Dev Drive can be created at a time. If one was
        // produced before reuse it.
        if (_devDrives.Count != 0)
        {
            _log.Information("Reusing existing Dev Drive");
            _devDrives.First().State = DevDriveState.New;
            if (!GetDevDriveValidationResults(_devDrives.First()).Contains(DevDriveValidationResult.Successful))
            {
                _devDrives.First().State = DevDriveState.Invalid;
            }

            return _devDrives.First();
        }

        var devDrive = GetDevDriveWithDefaultInfo();
        var driveTaskGroup = _host.GetService<SetupFlowOrchestrator>().GetTaskGroup<DevDriveTaskGroup>();
        if (driveTaskGroup != null)
        {
            ViewModel.TaskGroup = driveTaskGroup;
        }

        ViewModel.UpdateDevDriveInfo(devDrive);
        _devDrives.Add(devDrive);
        PreviousDevDrive = devDrive;

        return devDrive;
    }

    /// <inheritdoc/>
    public IEnumerable<IDevDrive> GetAllDevDrivesThatExistOnSystem()
    {
        try
        {
            using var session = CimSession.Create(null);
            var devDrives = new List<IDevDrive>();
            foreach (var queryObj in session.QueryInstances("root\\Microsoft\\Windows\\Storage", "WQL", "SELECT * FROM MSFT_Volume"))
            {
                var volumePath = queryObj.CimInstanceProperties["Path"].Value as string;
                var volumeLabel = queryObj.CimInstanceProperties["FileSystemLabel"].Value as string;
                var volumeSize = queryObj.CimInstanceProperties["Size"].Value;
                var volumeLetter = queryObj.CimInstanceProperties["DriveLetter"].Value;
                var volumeSizeRemaining = queryObj.CimInstanceProperties["SizeRemaining"].Value;
                uint outputSize;
                var volumeInfo = new FILE_FS_PERSISTENT_VOLUME_INFORMATION { };
                var inputVolumeInfo = new FILE_FS_PERSISTENT_VOLUME_INFORMATION { };
                inputVolumeInfo.FlagMask = _devDriveVolumeStateFlag | _devDriveVolumeStateTrustedFlag;
                inputVolumeInfo.Version = 1;

                SafeFileHandle volumeFileHandle = PInvoke.CreateFile(
                    volumePath,
                    (uint)(FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES | FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES),
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
                        volumeLetter is char newLetter && volumeSize is ulong newSize &&
                        volumeSizeRemaining is ulong newSizeRemaining)
                    {
                        var isInTerabytes = newSize >= DevDriveUtil.OneTbInBytes;
                        var newDevDrive = new Models.DevDrive
                        {
                            DriveLetter = newLetter,
                            DriveSizeInBytes = newSize,
                            DriveSizeRemainingInBytes = newSizeRemaining,
                            DriveUnitOfMeasure = isInTerabytes ? ByteUnit.TB : ByteUnit.GB,
                            DriveLocation = string.Empty,
                            DriveLabel = volumeLabel,
                            State = DevDriveState.ExistsOnSystem,
                            IsDevDriveTrusted = (volumeInfo.VolumeFlags & _devDriveVolumeStateTrustedFlag) > 0,
                        };

                        devDrives.Add(newDevDrive);
                    }
                }
            }

            return devDrives;
        }
        catch (Exception ex)
        {
            // Log then return empty list, don't show the user their existing dev drive. Not catastrophic failure.
            _log.Error(ex, $"Failed to get existing Dev Drives.");
            return new List<IDevDrive>();
        }
    }

    /// <summary>
    /// Gets prepopulated data and updates the passed in dev drive object with it.
    /// </summary>
    private DevDrive GetDevDriveWithDefaultInfo()
    {
        _log.Information("Setting default Dev Drive info");
        var root = Path.GetPathRoot(Environment.SystemDirectory);
        var validationSuccessful = true;

        try
        {
            var drive = new DriveInfo(root);
            if (DevDriveUtil.MinDevDriveSizeInBytes > (ulong)drive.AvailableFreeSpace)
            {
                _log.Error("Not enough space available to create a Dev Drive");
                TelemetryFactory.Get<ITelemetry>().Log(
                                              "DevDrive_Insufficient_DiskSpace",
                                              LogLevel.Critical,
                                              new EmptyEvent());
                validationSuccessful = false;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Unable to get available Free Space for {root}.");
            validationSuccessful = false;
        }

        var availableLetters = GetAvailableDriveLetters();
        var driveLetter = (availableLetters.Count == 0) ? '\0' : availableLetters[0];
        if (driveLetter == '\0')
        {
            _log.Error("No drive letters available to assign to Dev Drive");
            validationSuccessful = false;
        }

        uint numberToAppendToFileNameIfExists = 1;
        var fullPath = Path.Combine(DefaultDevDriveLocation, $"{_defaultVhdxName}.vhdx");
        var fileName = _defaultVhdxName;

        // If original default file name exists we'll increase the number next to the filename
        while (File.Exists(fullPath) && numberToAppendToFileNameIfExists <= MaxNumberToAppendToFileName)
        {
            fileName = $"{_defaultVhdxName} {numberToAppendToFileNameIfExists}";
            fullPath = Path.Combine(DefaultDevDriveLocation, $"{fileName}.vhdx");
            numberToAppendToFileNameIfExists++;
        }

        var devDriveState = validationSuccessful ? DevDriveState.New : DevDriveState.Invalid;
        return new Models.DevDrive(driveLetter, DevDriveUtil.MinDevDriveSizeInBytes, DevDriveUtil.MinDevDriveSizeInBytes /*size remaining*/, ByteUnit.GB, DefaultDevDriveLocation, fileName, devDriveState, Guid.NewGuid());
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

        if (GetAvailableDriveLetters().Count == 0)
        {
            returnSet.Add(DevDriveValidationResult.NoDriveLettersAvailable);
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

        // Check if the drive letter isn't already being used by another Dev Drive object in memory and if its an actual valid drive letter.
        if (!DevDriveUtil.IsCharValidDriveLetter(devDrive.DriveLetter) ||
            _devDrives.FirstOrDefault(drive => drive.DriveLetter == devDrive.DriveLetter && drive.ID != devDrive.ID) != null)
        {
            returnSet.Add(DevDriveValidationResult.DriveLetterNotAvailable);
        }

        var result = IsPathValid(devDrive);
        if (result != DevDriveValidationResult.Successful)
        {
            returnSet.Add(result);
        }

        try
        {
            foreach (var curDriveOnSystem in DriveInfo.GetDrives())
            {
                if (curDriveOnSystem.Name[0] == devDrive.DriveLetter)
                {
                    returnSet.Add(DevDriveValidationResult.DriveLetterNotAvailable);
                }

                // If drive location is invalid, we would have already captured this in the IsPathValid call above.
                var potentialRoot = string.IsNullOrEmpty(devDrive.DriveLocation) ? '\0' : char.ToUpperInvariant(devDrive.DriveLocation[0]);
                if (potentialRoot == curDriveOnSystem.Name[0] &&
                    curDriveOnSystem.IsReady &&
                    (devDrive.DriveSizeInBytes > (ulong)curDriveOnSystem.TotalFreeSpace))
                {
                    returnSet.Add(DevDriveValidationResult.NotEnoughFreeSpace);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to validate selected Drive letter ({devDrive.DriveLocation.FirstOrDefault()}).");
            returnSet.Add(DevDriveValidationResult.DriveLetterNotAvailable);
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
        try
        {
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
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get Available Drive letters.");
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
            locationRoot = Path.GetPathRoot(devDrive.DriveLocation);
            if (!string.IsNullOrEmpty(locationRoot) && Path.IsPathFullyQualified(devDrive.DriveLocation))
            {
                var isNetworkPath = false;
                unsafe
                {
                    fixed (char* tempPath = devDrive.DriveLocation)
                    {
                        isNetworkPath = PInvoke.PathIsNetworkPath(tempPath).Equals(new BOOL(true));
                    }
                }

                if (!isNetworkPath && File.Exists(Path.Combine(devDrive.DriveLocation, devDrive.DriveLabel + ".vhdx")))
                {
                    return DevDriveValidationResult.FileNameAlreadyExists;
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
        if (_devDrives.Count != 0)
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

    /// <inheritdoc/>
    public HashSet<char> DriveLettersInUseByDevDrivesCurrentlyOnSystem
    {
        get
        {
            var listOfLetters = new HashSet<char>();
            foreach (var devDrive in GetAllDevDrivesThatExistOnSystem())
            {
                listOfLetters.Add(devDrive.DriveLetter);
            }

            return listOfLetters;
        }
    }
}
