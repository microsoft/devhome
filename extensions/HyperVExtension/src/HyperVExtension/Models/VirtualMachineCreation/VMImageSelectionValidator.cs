// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using Windows.Win32;
using static HyperVExtension.Constants;
using static HyperVExtension.Helpers.BytesHelper;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Used to validate all prerequisites needed to create a VM from the VM gallery.
/// </summary>
public class VMImageSelectionValidator
{
    private readonly string _imageName;

    private readonly VMGalleryDisk _imageDiskInfo;

    private readonly IStringResource _stringResource;

    private readonly string _defaultVirtualDiskPath;

    private readonly List<Func<bool>> _validationMethods;

    private readonly string _defaultVirtualDiskDriveRootPath;

    private ulong _defaultVirtualDiskDriveFreeSpace;

    public string ErrorMessage { get; private set; } = string.Empty;

    public VMImageSelectionValidator(
        VMGalleryImage image,
        IStringResource stringResource,
        string defaultVirtualDiskLocation)
    {
        _imageName = image.Name;
        _imageDiskInfo = image.Disk;
        _stringResource = stringResource;
        _defaultVirtualDiskPath = defaultVirtualDiskLocation;
        _defaultVirtualDiskDriveRootPath = Path.GetPathRoot(defaultVirtualDiskLocation) ?? string.Empty;
        _validationMethods = new List<Func<bool>>()
        {
            ValidateDefaultVirtualDiskLocation,
            ValidateUserHasEnoughFreeSpace,
        };
    }

    /// <summary>
    /// To create a VM from the VM gallery we need to download an archive file that contains the virtual
    /// disk the OS image is stored on. The download is placed in the users temp folder and the extracted
    /// virtual disk file is placed in the users default Hyper-V virtual disk location. Note: the drive
    /// the default Hyper-V virtual disk is located on can be different from the drive the users temp folder
    /// is located on.
    /// </summary>
    private bool ValidateUserHasEnoughFreeSpace()
    {
        var systemDriveFreeSpace = (ulong)new DriveInfo(SystemRootPath).AvailableFreeSpace;
        var combinedSpaceRequired = _imageDiskInfo.ExtractedFileRequiredFreeSpace + _imageDiskInfo.ArchiveSizeInBytes;

        // The drive we save the archive to is the same drive the users default virtual disk path is
        // located on. So the required available space will only be for that drive.
        if (SystemRootPath.Equals(_defaultVirtualDiskDriveRootPath, StringComparison.OrdinalIgnoreCase) &&
            systemDriveFreeSpace < combinedSpaceRequired)
        {
            ErrorMessage = _stringResource.GetLocalized(
                "SpaceErrorForSystemAndDefaultHyperVSameLocation",
                ConvertBytesToString(combinedSpaceRequired - systemDriveFreeSpace),
                SystemRootPath,
                _imageName);
            return false;
        }

        // We have enough space to download the archive file to the users temp folder location but not enough
        // space to extract the virtual disk file to the users default virtual disk location.
        if (systemDriveFreeSpace >= _imageDiskInfo.ArchiveSizeInBytes &&
            _defaultVirtualDiskDriveFreeSpace < _imageDiskInfo.ExtractedFileRequiredFreeSpace)
        {
            ErrorMessage = _stringResource.GetLocalized(
                "SpaceErrorDefaultHyperVDrive",
                ConvertBytesToString(_imageDiskInfo.ExtractedFileRequiredFreeSpace - _defaultVirtualDiskDriveFreeSpace),
                _imageName,
                _defaultVirtualDiskPath);
            return false;
        }

        // We do not have enough space to download the archive file to the users temp folder location.
        // But we have enough space to extract the virtual disk file to the users default virtual disk location.
        if (systemDriveFreeSpace < _imageDiskInfo.ArchiveSizeInBytes &&
            _defaultVirtualDiskDriveFreeSpace >= _imageDiskInfo.ExtractedFileRequiredFreeSpace)
        {
            ErrorMessage = _stringResource.GetLocalized(
                "SpaceErrorSystemDrive",
                ConvertBytesToString(_imageDiskInfo.ArchiveSizeInBytes - systemDriveFreeSpace),
                _imageName,
                SystemRootPath);
            return false;
        }

        // We do not have enough space to download the archive file to the users temp folder location.
        // We also do not have enough space to extract the virtual disk file to the users default virtual disk location.
        if (systemDriveFreeSpace < _imageDiskInfo.ArchiveSizeInBytes &&
            _defaultVirtualDiskDriveFreeSpace < _imageDiskInfo.ExtractedFileRequiredFreeSpace)
        {
            ErrorMessage = _stringResource.GetLocalized(
                "SpaceErrorForSystemAndDefaultHyperVDifferentLocations",
                ConvertBytesToString(_imageDiskInfo.ArchiveSizeInBytes - systemDriveFreeSpace),
                SystemRootPath,
                _imageName,
                ConvertBytesToString(_imageDiskInfo.ExtractedFileRequiredFreeSpace - _defaultVirtualDiskDriveFreeSpace),
                _defaultVirtualDiskPath);
            return false;
        }

        return true;
    }

    private unsafe bool ValidateDefaultVirtualDiskLocation()
    {
        var pathDoesNotExist = !Directory.Exists(_defaultVirtualDiskPath);
        ulong availableFreeSpace;
        var cantGetDriveFreeSpace = !PInvoke.GetDiskFreeSpaceEx(_defaultVirtualDiskPath, null, null, &availableFreeSpace);

        if (pathDoesNotExist || cantGetDriveFreeSpace)
        {
            var resourceKey = pathDoesNotExist
                ? "DefaultVirtualDiskPathDoesNotExist"
                : "UnableToGetSizeOfDefaultVirtualDiskPathDrive";

            ErrorMessage = _stringResource.GetLocalized(
                resourceKey,
                _defaultVirtualDiskPath,
                ConvertBytesToString(_imageDiskInfo.ExtractedFileRequiredFreeSpace),
                _imageName);
            return false;
        }

        _defaultVirtualDiskDriveFreeSpace = availableFreeSpace;

        return true;
    }

    /// <summary>
    /// Validates that the selected VM can be created on users machine without issue.
    /// </summary>
    /// <returns>
    /// True only when all prerequisites to create the virtual machine has been
    /// met. False otherwise.
    /// </returns>
    public bool Validate()
    {
        // The Any() method is sequential
        return !_validationMethods.Any(method => method() == false);
    }
}
