// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.SetupFlow.Common.DevDriveFormatter;
using Microsoft.Win32.SafeHandles;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.Vhd;
using Windows.Win32.System.Ioctl;

namespace DevHome.SetupFlow.ElevatedComponent.Tasks;

/// <summary>
/// Class that will perform storage operations related to Dev Drives.
/// </summary>
public sealed class DevDriveStorageOperator
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevDriveStorageOperator));

    /// <summary>
    /// Windows already uses 1024 bytes to represent a Kilobyte, so we'll stick with this.
    /// </summary>
    public static readonly long _oneKb = 1024;
    public static readonly long _oneMb = _oneKb * _oneKb;

    /// <summary>
    /// Store partition information, when IoDeviceControl is called with IOCTL_DISK_GET_DRIVE_LAYOUT_EX.
    /// </summary>
    internal struct PartitionLayout
    {
        internal DRIVE_LAYOUT_INFORMATION_EX Info;

        internal PARTITION_INFORMATION_EX[] Partitions => new PARTITION_INFORMATION_EX[1];
    }

    /// <summary>
    /// Manually make this macro because CsWin32 does not generate it from ntddisk.h.
    /// The IOCTL_DISK_ARE_VOLUMES_READY when used with ioDeviceControl, is used so that DevHome
    /// waits until all volumes have completed any work assigned to them before using them.
    /// e.g creating the partition, before trying to use it again.
    /// see https://learn.microsoft.com/windows/win32/fileio/ioctl-disk-are-volumes-ready
    /// https://learn.microsoft.com/windows/win32/fileio/disk-management-control-codes
    /// </summary>
    private readonly struct IOCTL_DISK_ARE_VOLUMES_READY
    {
        public static readonly int _deviceType = 0x00000007;
        public static readonly int _function = 0x0087;
        public static readonly int _access = 0x0001;

        public static uint CtlCodeOutput => (uint)(_deviceType << 16 | _access << 14 | _function << 2);
    }

    // Signals to the system to reattach the virtual disk at boot time. This flag will be present in the public
    // virtdisk.h file in the Windows SDK on systems that have it. For now since CsWin32 will not find this, manually add it.
    // This will be documented here: https://learn.microsoft.com/windows/win32/api/virtdisk/ne-virtdisk-attach_virtual_disk_flag
    // This is only temporary, and will be removed once we can use it with CSWin32 out of the box.
    private const uint AttachVirtualDiskFlagAtBoot = 0x00000400;

    public DevDriveStorageOperator()
    {
    }

    /// <summary>
    /// Callers wanting to create a Dev Drive should call this method. The createDevDrive method kicks off
    /// all the operations and methods needed to create a Dev Drive. Note: The implementation of this may
    /// change.
    /// </summary>
    /// <param name="virtDiskPath">The place in the file system the vhdx file will be saved to</param>
    /// <param name="sizeInBytes">The size the drive will be created with</param>
    /// <param name="newDriveLetter">The drive letter to format the new drive</param>
    /// <param name="driveLabel">The label that will be given to the drive during formatting</param>
    /// <returns>An int which is the HRESULT code that indicates whether the operation succeeded or failed</returns>
    public int CreateDevDrive(string virtDiskPath, ulong sizeInBytes, char newDriveLetter, string driveLabel)
    {
        // Create the location if it doesn't exist.
        var location = Path.GetDirectoryName(virtDiskPath);
        if (!string.IsNullOrEmpty(location) && !Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }

        string virtDiskPhysicalPath;
        var result = CreateAndAttachVhdx(virtDiskPath, sizeInBytes, out virtDiskPhysicalPath);
        if (result.Failed)
        {
            DetachVirtualDisk(virtDiskPath);
            return result.Value;
        }

        uint diskNumber;
        result = CreatePartition(virtDiskPhysicalPath, out diskNumber);
        if (result.Failed)
        {
            DetachVirtualDisk(virtDiskPath);
            return result.Value;
        }

        result = AssignDriveLetterToPartition(diskNumber, newDriveLetter);
        if (result.Failed)
        {
            DetachVirtualDisk(virtDiskPath);
            return result.Value;
        }

        var finishedResult = FormatPartitionAsDevDrive(newDriveLetter, driveLabel);
        if (finishedResult != 0)
        {
            DetachVirtualDisk(virtDiskPath);
        }

        return finishedResult;
    }

    /// <summary>
    /// Creates the virtual disk and also attaches it so that its lifespan is permanent and survives reboots.
    /// </summary>
    /// <param name="virtDiskPath">The place in the file system the vhdx file will be saved to</param>
    /// <param name="sizeInBytes">The size the drive will be created with</param>
    /// <param name="virtDiskPhysicalPath">The logical representation of the virtual disks physical path on the system</param>
    /// <returns>An HRESULT that indicates whether the operation succeeded or failed</returns>
    private HRESULT CreateAndAttachVhdx(string virtDiskPath, ulong sizeInBytes, out string virtDiskPhysicalPath)
    {
        virtDiskPhysicalPath = string.Empty;

        // Create the initial dynamically resizing virtual disk.
        var vhdParams = new CREATE_VIRTUAL_DISK_PARAMETERS
        {
            Version = CREATE_VIRTUAL_DISK_VERSION.CREATE_VIRTUAL_DISK_VERSION_2,
        };

        vhdParams.Anonymous.Version2.MaximumSize = sizeInBytes;
        var storageType = new VIRTUAL_STORAGE_TYPE
        {
            VendorId = PInvoke.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT,
            DeviceId = PInvoke.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX,
        };

        _log.Information($"Starting CreateVirtualDisk");
        SafeFileHandle tempHandle;
        var result = PInvoke.CreateVirtualDisk(
            storageType,
            virtDiskPath,
            VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_NONE,
            (PSECURITY_DESCRIPTOR)null,
            CREATE_VIRTUAL_DISK_FLAG.CREATE_VIRTUAL_DISK_FLAG_NONE,
            0,
            vhdParams,
            null,
            out tempHandle);
        if (result != WIN32_ERROR.NO_ERROR)
        {
            _log.Error($"CreateVirtualDisk failed with error: {PInvoke.HRESULT_FROM_WIN32(result):X}");
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        _log.Information($"Starting AttachVirtualDisk");

        result = PInvoke.AttachVirtualDisk(
            tempHandle,
            (PSECURITY_DESCRIPTOR)null,
            ATTACH_VIRTUAL_DISK_FLAG.ATTACH_VIRTUAL_DISK_FLAG_PERMANENT_LIFETIME | (ATTACH_VIRTUAL_DISK_FLAG)AttachVirtualDiskFlagAtBoot,
            0,
            null,
            null);

        if (result != WIN32_ERROR.NO_ERROR)
        {
            _log.Error($"AttachVirtualDisk failed with error: {PInvoke.HRESULT_FROM_WIN32(result):X}");
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        _log.Information($"Starting GetVirtualDiskPhysicalPath");

        // Getting the virtual disk path here before exiting save a lot more repeated win32 code in the long run
        // as we need the path to get the disk number.
        var tempPhysicalPath = new string('\0', (int)PInvoke.MAX_PATH);
        var virtDiskPhysicalPathSize = PInvoke.MAX_PATH * sizeof(char);
        unsafe
        {
            fixed (char* pathPtr = tempPhysicalPath)
            {
                result = PInvoke.GetVirtualDiskPhysicalPath(
                    tempHandle,
                    ref virtDiskPhysicalPathSize,
                    pathPtr);
                if (result != WIN32_ERROR.NO_ERROR)
                {
                    _log.Error($"GetVirtualDiskPhysicalPath failed with error: {PInvoke.HRESULT_FROM_WIN32(result):X}");
                    return PInvoke.HRESULT_FROM_WIN32(result);
                }
            }
        }

        virtDiskPhysicalPath = new string(tempPhysicalPath);
        tempHandle.Close();
        return PInvoke.HRESULT_FROM_WIN32(result);
    }

    /// <summary>
    /// Creates a single new Guid Partition table style partition on the virtual disk.
    /// </summary>
    /// <param name="virtDiskPhysicalPath">The logical representation of the virtual disks physical path on the system</param>
    /// <param name="diskNumber">The current number of the new virtual disk</param>
    /// <returns>An HRESULT that indicates whether the operation succeeded or failed</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:Statement should not use unnecessary parenthesis", Justification = "For math, 10 - 3 - 7 is a lot different than 10  - (3 - 7)")]
    private HRESULT CreatePartition(string virtDiskPhysicalPath, out uint diskNumber)
    {
        diskNumber = 0;
        HRESULT error;
        _log.Information($"Starting CreateFile from physical path");
        var diskHandle = PInvoke.CreateFile(
            virtDiskPhysicalPath,
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            0,
            null);

        if (diskHandle.IsInvalid)
        {
            error = ReturnLastErrorAsHR();
            _log.Error($"CreateFile from physical path: {virtDiskPhysicalPath.Trim('\0')} failed with error: {error:X}, is diskHandle invalid: {diskHandle.IsInvalid}");
            return error;
        }

        _log.Information($"Starting Initialize disk");

        // Initialize the disk
        CREATE_DISK createDisk;
        createDisk.PartitionStyle = PARTITION_STYLE.PARTITION_STYLE_GPT;
        uint unusedBytes;
        var result = new BOOL(1);
        unsafe
        {
            result = PInvoke.DeviceIoControl(
            diskHandle,
            PInvoke.IOCTL_DISK_CREATE_DISK,
            &createDisk,
            (uint)sizeof(CREATE_DISK),
            null,
            0,
            &unusedBytes,
            null);
        }

        if (!result)
        {
            error = ReturnLastErrorAsHR();
            _log.Error($"DeviceIoControl initialize disk failed with error: {error:X}, is diskHandle invalid: {diskHandle.IsInvalid}");
            return error;
        }

        _log.Information($"Getting partition layout");

        // Collect information about how the partition layout looks before
        // attempting to create it.
        PartitionLayout partitionLayout;
        unsafe
        {
            result = PInvoke.DeviceIoControl(
                diskHandle,
                PInvoke.IOCTL_DISK_GET_DRIVE_LAYOUT_EX,
                null,
                0,
                &partitionLayout,
                (uint)sizeof(PartitionLayout),
                &unusedBytes,
                null);
        }

        if (!result)
        {
            error = ReturnLastErrorAsHR();
            _log.Error($"DeviceIoControl get partition layout failed with error: {error:X}, is diskHandle invalid: {diskHandle.IsInvalid}");
            return error;
        }

        _log.Information($"Setting partition layout");
        unsafe
        {
            partitionLayout.Info.PartitionCount = 1;
            var partitionInfo = &partitionLayout.Info.PartitionEntry.e0;
            partitionInfo->PartitionStyle = PARTITION_STYLE.PARTITION_STYLE_GPT;

            // There are currently no partitions on the disk.  Start off the
            // first partition to have an offset of 1024Kb, 1 megabyte. The StartingUsableOffset is set by windows and can't change it.
            // But we can use it to make sure the starting offset and partition
            // length are 1Mb aligned. If you open the disk part app and select any disk and look at the partitions, you will
            // see the same thing done there. The goal here is to round the starting offset up to the nearest Mb and partition
            // length rounded down to the nearest Mb to keep this alignment.
            var totalLengthInBytes = partitionLayout.Info.Anonymous.Gpt.UsableLength;
            partitionInfo->StartingOffset = ((partitionLayout.Info.Anonymous.Gpt.StartingUsableOffset / _oneMb) + 1) * _oneMb;
            totalLengthInBytes -= partitionInfo->StartingOffset - partitionLayout.Info.Anonymous.Gpt.StartingUsableOffset;
            partitionInfo->PartitionLength = totalLengthInBytes / _oneMb * _oneMb;
            partitionInfo->PartitionNumber = 0;
            partitionInfo->RewritePartition = new BOOLEAN(1);
            partitionInfo->Anonymous.Gpt.PartitionType = PInvoke.PARTITION_BASIC_DATA_GUID;

            result = PInvoke.DeviceIoControl(
                diskHandle,
                PInvoke.IOCTL_DISK_SET_DRIVE_LAYOUT_EX,
                &partitionLayout,
                (uint)sizeof(PartitionLayout),
                null,
                0,
                &unusedBytes,
                null);
        }

        if (!result)
        {
            error = ReturnLastErrorAsHR();
            _log.Error($"DeviceIoControl set partition layout failed with error: {error:X}, is diskHandle invalid: {diskHandle.IsInvalid}");
            return error;
        }

        _log.Information($"Waiting on IOCTL_DISK_ARE_VOLUMES_READY");

        // After the partition is created, wait for the volume
        // to fully install. IOCTL_DISK_ARE_VOLUMES_READY waits
        // for all volumes on the specified disk to be ready for use
        unsafe
        {
            result = PInvoke.DeviceIoControl(
                diskHandle,
                IOCTL_DISK_ARE_VOLUMES_READY.CtlCodeOutput,
                null,
                0,
                null,
                0,
                null,
                null);
        }

        if (!result)
        {
            error = ReturnLastErrorAsHR();
            _log.Error($"DeviceIoControl set IOCTL_DISK_ARE_VOLUMES_READY failed with error: {error:X}, is diskHandle invalid: {diskHandle.IsInvalid}");
            return error;
        }

        _log.Information($"Getting the virtual disks disk number");

        // At this point the partition has been created with the first assignable
        // drive letter by windows. Use the disk number so the drive letter can be changed to a
        // drive letter the user wants.
        var storageDeviceInfo = new STORAGE_DEVICE_NUMBER_EX { };
        unsafe
        {
            uint bytesReturned;
            result = PInvoke.DeviceIoControl(
                diskHandle,
                PInvoke.IOCTL_STORAGE_GET_DEVICE_NUMBER_EX,
                null,
                0,
                &storageDeviceInfo,
                (uint)sizeof(STORAGE_DEVICE_NUMBER_EX),
                &bytesReturned,
                null);
        }

        if (!result)
        {
            error = ReturnLastErrorAsHR();
            _log.Error($"DeviceIoControl get device number failed with error: {error:X}, is diskHandle invalid: {diskHandle.IsInvalid}");
            return error;
        }

        diskNumber = storageDeviceInfo.DeviceNumber;
        return new HRESULT(0);
    }

    /// <summary>
    /// Performs the operations needed to update the auto generated drive letter to one that is provided by a caller.
    /// </summary>
    /// <param name="diskNumber">The disk number the method uses to located the correct disk</param>
    /// <param name="newDriveLetter">The new drive letter provided by the caller</param>
    /// <returns>An HRESULT that indicates whether the operation succeeded or failed</returns>
    private HRESULT AssignDriveLetterToPartition(uint diskNumber, char newDriveLetter)
    {
        // Just created the virtual disk and created a single partition. Don't have to worry about there
        // being multiple on the disk. However in win32 to get the correct volume, FindFirstVolume is still needed
        // which iterates through each volume on the system, and then compares the disk that volume is on with the virtual disk.
        // That is how we'll know which volume is the right one.
        HRESULT error;
        unsafe
        {
            var volumeGuidPathBeforeTrim = new string('\0', (int)PInvoke.MAX_PATH);
            FindVolumeCloseSafeHandle volumeHandle;
            fixed (char* pathPtr = volumeGuidPathBeforeTrim)
            {
                _log.Information($"Finding to first volume");
                volumeHandle = new FindVolumeCloseSafeHandle(PInvoke.FindFirstVolume(pathPtr, PInvoke.MAX_PATH));
                if (volumeHandle.IsInvalid)
                {
                    error = ReturnLastErrorAsHR();
                    _log.Error($"FindFirstVolume failed with error: {error:X}");
                    return error;
                }

                _log.Information($"First volume found, volume guid {volumeGuidPathBeforeTrim}");
                do
                {
                    // The call to createFile succeeds with the trailing backslash, however when getting the device info
                    // using that handle fails. Remove it before the call, and add it back later.
                    var volumeGuidPathAfterTrim = volumeGuidPathBeforeTrim.Trim('\0');
                    var hasTrailingBackslash = volumeGuidPathAfterTrim.Last() == '\\';
                    if (hasTrailingBackslash)
                    {
                        volumeGuidPathAfterTrim = volumeGuidPathAfterTrim.TrimEnd('\\');
                    }

                    _log.Information($"Creating volume file handle for volume volume guid {volumeGuidPathAfterTrim}");
                    var volumeFileHandle = PInvoke.CreateFile(
                        volumeGuidPathAfterTrim,
                        0,
                        FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                        null,
                        FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                        0,
                        null);

                    if (volumeFileHandle.IsInvalid)
                    {
                        error = ReturnLastErrorAsHR();
                        _log.Error($"CreateFile for volume guid {volumeGuidPathAfterTrim} failed with error: {error:X}");
                        return error;
                    }

                    _log.Information($"Getting disk number for {volumeGuidPathAfterTrim}");

                    // The device number will tell us if it is a virtual disk or not.
                    var deviceInfo = new STORAGE_DEVICE_NUMBER_EX { };
                    uint unusedBytes;
                    var result = PInvoke.DeviceIoControl(
                        volumeFileHandle,
                        PInvoke.IOCTL_STORAGE_GET_DEVICE_NUMBER_EX,
                        null,
                        0,
                        &deviceInfo,
                        (uint)sizeof(STORAGE_DEVICE_NUMBER_EX),
                        &unusedBytes,
                        null);
                    if (!result)
                    {
                        error = ReturnLastErrorAsHR();
                        _log.Warning($"DeviceIoControl getting disk number for volume guid {volumeGuidPathAfterTrim} failed with error: {error:X}, continuing to other volumes...");
                        continue;
                    }

                    _log.Information($"Comparing disk number: {deviceInfo.DeviceNumber} for volume: {volumeGuidPathAfterTrim} with virtual disks disk number: {diskNumber}");

                    // Only the virtual disk created above will have this disk number,
                    // the disk number is guaranteed to be the same until a reboot, which is fine
                    // since we use it immediately.
                    if (diskNumber != deviceInfo.DeviceNumber)
                    {
                        _log.Information($"volume guid {volumeGuidPathAfterTrim} on device number: {deviceInfo.DeviceNumber} does not have the same device number as the newly created virtual disk's device number {diskNumber}");
                        continue;
                    }

                    volumeGuidPathBeforeTrim = hasTrailingBackslash ? volumeGuidPathBeforeTrim : volumeGuidPathAfterTrim;
                    _log.Information($"Finding old drive letter for volume: {volumeGuidPathBeforeTrim}");

                    // At this point there will most likely be a default drive letter
                    // if available that was given to the drive, remove it,
                    // or else setting it will fail.
                    var oldDriveLetterPath = new string('\0', (int)PInvoke.MAX_PATH);
                    uint oldDrivePathLength;
                    fixed (char* drivePtr = oldDriveLetterPath)
                    {
                        result = PInvoke.GetVolumePathNamesForVolumeName(
                            volumeGuidPathBeforeTrim,
                            drivePtr,
                            PInvoke.MAX_PATH,
                            out oldDrivePathLength);
                        if (!result)
                        {
                            error = ReturnLastErrorAsHR();
                            _log.Error($"GetVolumePathNamesForVolumeName failed with error: {error:X}, vhdx disk number {diskNumber}, volume guid {volumeGuidPathBeforeTrim}");
                            return error;
                        }
                    }

                    _log.Information($"Deleting old drive letter for volume: {volumeGuidPathBeforeTrim}");
                    if (oldDriveLetterPath[0] != '\0')
                    {
                        result = PInvoke.DeleteVolumeMountPoint(oldDriveLetterPath);
                        if (!result)
                        {
                            error = ReturnLastErrorAsHR();
                            _log.Error($"DeleteVolumeMountPoint failed with error: {error:X}, vhdx disk number {diskNumber}, old Drive Letter: {oldDriveLetterPath[0]}, volume guid {volumeGuidPathBeforeTrim}");
                            return error;
                        }
                    }

                    _log.Information($"Setting {newDriveLetter}: as new drive letter for volume: {volumeGuidPathBeforeTrim}");

                    // SetVolumeMountPoint expects the drive letter to be in the form of Letter:\ e.g "A:\"
                    var fullDrivePath = newDriveLetter + ":\\";
                    result = PInvoke.SetVolumeMountPoint(fullDrivePath, volumeGuidPathBeforeTrim);
                    if (!result)
                    {
                        error = ReturnLastErrorAsHR();
                        _log.Error($"SetVolumeMountPoint failed with error: {error:X}, vhdx disk number {diskNumber}, old Drive Letter: {oldDriveLetterPath[0]}, attempted new Drive path {fullDrivePath}, volume guid {volumeGuidPathBeforeTrim}");
                        return error;
                    }

                    return new HRESULT(0);
                }
                while (PInvoke.FindNextVolume(volumeHandle, pathPtr, PInvoke.MAX_PATH));
            }
        }

        // FindNextVolume failed for some other reason without us finding our volume.
        error = ReturnLastErrorAsHR();
        _log.Error($"Failed to find the new volume on disk number {diskNumber} error: {error:X}");
        return error;
    }

    /// <summary>
    /// Helper method that results in the HRESULT class wrapping GetHRForLastWin32Error return value.
    /// </summary>
    /// <returns>Returns error code indicating success or failure</returns>
    private HRESULT ReturnLastErrorAsHR()
    {
        return new HRESULT(Marshal.GetHRForLastWin32Error());
    }

    /// <summary>
    /// Uses WMI to and the storage api to format the drive as a Dev Drive. Note: the implementation
    /// is subject to change in the future.
    /// </summary>
    /// <param name="curDriveLetter">The drive letter the method will use when attempting to find a volume and format it</param>
    /// <param name="driveLabel">The new drive label the Dev Drive will have after formatting completes.</param>
    /// <returns>An HRESULT that indicates whether the operation succeeded or failed</returns>
    private int FormatPartitionAsDevDrive(char curDriveLetter, string driveLabel)
    {
        _log.Information($"Creating DevDriveFormatter");
        var devDriveFormatter = new DevDriveFormatter();
        return devDriveFormatter.FormatPartitionAsDevDrive(curDriveLetter, driveLabel);
    }

    /// <summary>
    /// Detaches the virtual disk and removes the vhdx file associated with it.
    /// </summary>
    /// <param name="virtDiskPath">The path in the file system to the vhdx file</param>
    private void DetachVirtualDisk(string virtDiskPath)
    {
        if (File.Exists(virtDiskPath))
        {
            var vhdParams = new OPEN_VIRTUAL_DISK_PARAMETERS
            {
                Version = OPEN_VIRTUAL_DISK_VERSION.OPEN_VIRTUAL_DISK_VERSION_2,
            };

            var storageType = new VIRTUAL_STORAGE_TYPE
            {
                VendorId = PInvoke.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT,
                DeviceId = PInvoke.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX,
            };

            SafeFileHandle tempHandle;
            var result = PInvoke.OpenVirtualDisk(
                storageType,
                virtDiskPath,
                VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_NONE,
                OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE,
                vhdParams,
                out tempHandle);

            // Pass through errors here and after the detach call below instead of returning immediately because there are instances
            // where the virtual disk file is created but not mounted. So in those instance a failure from OpenVirtualDisk and DetachVirtualDisk are expected.
            if (result != WIN32_ERROR.NO_ERROR)
            {
                _log.Error($"OpenVirtualDisk failed with error: {PInvoke.HRESULT_FROM_WIN32(result):X}");
            }

            result = PInvoke.DetachVirtualDisk(
                tempHandle,
                DETACH_VIRTUAL_DISK_FLAG.DETACH_VIRTUAL_DISK_FLAG_NONE,
                0);

            if (result != WIN32_ERROR.NO_ERROR)
            {
                _log.Error($"DetachVirtualDisk failed with error: {PInvoke.HRESULT_FROM_WIN32(result):X}");
            }

            tempHandle.Close();
            File.Delete(virtDiskPath);
        }
    }
}
