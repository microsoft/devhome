// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.Vhd;
using Windows.Win32.System.Ioctl;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Class that will perform storage operations related to Dev Drives.
/// </summary>
public sealed class DevDriveStorageOperator
{
    /// <summary>
    /// Windows already uses 1024 bytes to represent a Kilobyte, so we'll stick with this.
    /// </summary>
    public static readonly long _oneKb = 1024;
    public static readonly long _oneMb = _oneKb * _oneKb;
    public static readonly long _fourKb = _oneKb * 4;
    public static readonly string _fileSytem = "ReFS";

    /// <summary>
    /// We need a way to hold the partition information, when we call IoDeviceControl with IOCTL_DISK_GET_DRIVE_LAYOUT_EX.
    /// </summary>
    internal struct PartitionLayout
    {
        internal DRIVE_LAYOUT_INFORMATION_EX Info;

        internal PARTITION_INFORMATION_EX[] Partitions => new PARTITION_INFORMATION_EX[1];
    }

    /// <summary>
    /// Since CsWin32 doesn't generate this macro from Ntdddisk.h we need to do it manually.
    /// The IOCTL_DISK_ARE_VOLUMES_READY when used with ioDeviceControl, is used so that we
    /// wait until all volumes have completed any work assigned to them before using them.
    /// e.g creating the partition, before trying to use it again.
    /// see https://learn.microsoft.com/en-us/windows/win32/fileio/ioctl-disk-are-volumes-ready
    /// https://learn.microsoft.com/en-us/windows/win32/fileio/disk-management-control-codes
    /// </summary>
    private struct IOCTL_DISK_ARE_VOLUMES_READY
    {
        public static readonly int _deviceType = 0x00000007;
        public static readonly int _function = 0x0087;
        public static readonly int _access = 0x0001;

        public static uint CtlCodeOutput => (uint)((_deviceType << 16) | (_access << 14) | (_function << 2));
   }

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
    /// <returns>An int which is the Hresult code that indicates whether the operation succeeded or failed</returns>
    public int CreateDevDrive(string virtDiskPath, ulong sizeInBytes, char newDriveLetter, string driveLabel)
    {
        string virtDiskPhysicalPath;
        var result = CreateAndAttachVhdx(virtDiskPath, sizeInBytes, out virtDiskPhysicalPath);
        if (result.Failed)
        {
            return result.Value;
        }

        uint diskNumber;
        result = CreatePartition(virtDiskPhysicalPath, out diskNumber);
        if (result.Failed)
        {
            return result.Value;
        }

        result = AssignDriveLetterToPartition(diskNumber, newDriveLetter);
        if (result.Failed)
        {
            return result.Value;
        }

        return FormatPartitionAsDevDrive(newDriveLetter, driveLabel).Value;
    }

    /// <summary>
    /// Creates the virtual disk and also attaches it so that its lifespan is permanant and survives reboots.
    /// </summary>
    /// <param name="virtDiskPath">The place in the file system the vhdx file will be saved to</param>
    /// <param name="sizeInBytes">The size the drive will be created with</param>
    /// <param name="virtDiskPhysicalPath">The logical representation of the virtual disks physical path on the system</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
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
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        result = PInvoke.AttachVirtualDisk(
            tempHandle,
            (PSECURITY_DESCRIPTOR)null,
            ATTACH_VIRTUAL_DISK_FLAG.ATTACH_VIRTUAL_DISK_FLAG_PERMANENT_LIFETIME,
            0,
            null,
            null);
        if (result != WIN32_ERROR.NO_ERROR)
        {
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        // Getting the virtual disk path here before exiting save alot more repeated win32 code in the long run
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
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:Statement should not use unnecessary parenthesis", Justification = "For math, 10 - 3 - 7 is alot different than 10  - (3 - 7)")]
    private HRESULT CreatePartition(string virtDiskPhysicalPath, out uint diskNumber)
    {
        diskNumber = 0;
        SafeFileHandle diskHandle = PInvoke.CreateFile(
            virtDiskPhysicalPath,
            FILE_ACCESS_FLAGS.FILE_GENERIC_READ | FILE_ACCESS_FLAGS.FILE_GENERIC_WRITE,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            0,
            null);

        if (diskHandle.IsInvalid)
        {
            return ReturnLastErrorAsHR();
        }

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
            ReturnLastErrorAsHR();
        }

        // Collect information about how we want the partition layout to look before
        // we attempt to create it.
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
            return ReturnLastErrorAsHR();
        }

        unsafe
        {
            partitionLayout.Info.PartitionCount = 1;
            PARTITION_INFORMATION_EX* partitionInfo = &partitionLayout.Info.PartitionEntry._0;
            partitionInfo->PartitionStyle = PARTITION_STYLE.PARTITION_STYLE_GPT;

            // There are currently no partitions on the disk, so to make this easy for us, we start off the
            // first partition to have an offset of 1024Kb, so 1 megabyte. The StartingUsableOffset is set by windows
            // so we don't have control over that. But we can use it to make sure the starting offset and partition
            // length are 1Mb aligned. If you open the disk part app and select any disk and look at the partitions, you will
            // see the same thing done there. The goal here is to round the starting offset up to the nearest Mb and partition
            // length rounded down to the nearest Mb to keep this alignment.
            var totalLengthInBytes = partitionLayout.Info.Anonymous.Gpt.UsableLength;
            partitionInfo->StartingOffset = ((partitionLayout.Info.Anonymous.Gpt.StartingUsableOffset / _oneMb) + 1) * _oneMb;
            totalLengthInBytes -= (partitionInfo->StartingOffset - partitionLayout.Info.Anonymous.Gpt.StartingUsableOffset);
            partitionInfo->PartitionLength = (totalLengthInBytes / _oneMb) * _oneMb;
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
            return ReturnLastErrorAsHR();
        }

        // After the partition is created, we need to wait for the volume
        // to become fully installed. IOCTL_DISK_ARE_VOLUMES_READY waits
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
            return ReturnLastErrorAsHR();
        }

        // At this point the partition has been created with the first assignable
        // drive letter by windows. We need to use the disk number so we can change
        // this drive letter to the one the user wants.
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
            return ReturnLastErrorAsHR();
        }

        diskNumber = storageDeviceInfo.DeviceNumber;
        return new HRESULT(0);
    }

    /// <summary>
    /// Performs the operations needed to update the auto generated drive letter to one that is provided by a caller.
    /// </summary>
    /// <param name="diskNumber">The disk number the method uses to located the correct disk</param>
    /// <param name="newDriveLetter">The new drive letter provided by the caller</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    private HRESULT AssignDriveLetterToPartition(uint diskNumber, char newDriveLetter)
    {
        // Since we have just created the virtual disk and created a single partition, we don't have to worry about there
        // being multiple on the disk. However in win32 to get the correct volume, we still need to use the FindFirstVolume
        // which iterates through each volume on the system, and then compares the disk that volume is on with the virtual disk.
        // Thats how we'll know which volume is the right one.
        unsafe
        {
            var volumeGuidPathBeforeTrim = new string('\0', (int)PInvoke.MAX_PATH);
            FindVolumeCloseSafeHandle volumeHandle;
            fixed (char* pathPtr = volumeGuidPathBeforeTrim)
            {
                volumeHandle = new FindVolumeCloseSafeHandle(PInvoke.FindFirstVolume(pathPtr, PInvoke.MAX_PATH));
                if (volumeHandle.IsInvalid)
                {
                    return ReturnLastErrorAsHR();
                }

                do
                {
                    // The call to createFile succeeds with the trailing backslash, however when getting the device info
                    // using that handle, it fails so we need to remove it before the call, and add it back later.
                    var volumeGuidPathAfterTrim = volumeGuidPathBeforeTrim.Trim('\0');
                    var hasTrailingbackslash = volumeGuidPathAfterTrim.Last() == '\\' ? true : false;
                    if (hasTrailingbackslash)
                    {
                        volumeGuidPathAfterTrim = volumeGuidPathAfterTrim.TrimEnd('\\');
                    }

                    SafeFileHandle volumeFileHandle = PInvoke.CreateFile(
                        volumeGuidPathAfterTrim,
                        0,
                        FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                        null,
                        FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                        0,
                        null);

                    if (volumeFileHandle.IsInvalid)
                    {
                        return ReturnLastErrorAsHR();
                    }

                    // The device number will tell us if we're looking at the virtual disk or not.
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
                        continue;
                    }

                    // Only the virtual disk that we created above will have this disk number,
                    // the disk number is guaranteed to be the same until a reboot, which is fine
                    // since we use it immediately.
                    if (diskNumber != deviceInfo.DeviceNumber)
                    {
                        continue;
                    }

                    volumeGuidPathBeforeTrim = hasTrailingbackslash ? volumeGuidPathBeforeTrim : volumeGuidPathAfterTrim;

                    // At this point there will most likely be a default drive letter
                    // if available that was given to the drive, so we need to remove it,
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
                            return ReturnLastErrorAsHR();
                        }
                    }

                    if (oldDriveLetterPath[0] != '\0')
                    {
                        result = PInvoke.DeleteVolumeMountPoint(oldDriveLetterPath);
                        if (!result)
                        {
                            return ReturnLastErrorAsHR();
                        }
                    }

                    // SetVolumeMountPoint expects the drive letter to be in the form of Letter:\ e.g "A:\"
                    var fullDrivePath = newDriveLetter + ":\\";
                    result = PInvoke.SetVolumeMountPoint(fullDrivePath, volumeGuidPathBeforeTrim);
                    if (!result)
                    {
                        return ReturnLastErrorAsHR();
                    }

                    return new HRESULT(0);
                }
                while (PInvoke.FindNextVolume(volumeHandle, pathPtr, PInvoke.MAX_PATH));
            }
        }

        // FindNextVolume errored out for some other reason without us finding our volume.
        return ReturnLastErrorAsHR();
    }

    /// <summary>
    /// Helper method that results in the the HRESULT class wrapping GetHRForLastWin32Error return value.
    /// </summary>
    /// <returns>Returns errorcode indicating success or failure</returns>
    private HRESULT ReturnLastErrorAsHR()
    {
        return new HRESULT(Marshal.GetHRForLastWin32Error());
    }

    /// <summary>
    /// Uses WMI to and the storage Api to format the drive as a Dev Drive. Note: the implementation
    /// is subject to change in the future.
    /// </summary>
    /// <param name="curDriveLetter">The drive letter the method will use when attempting to find a volume and format it</param>
    /// <param name="driveLabel">The new drive label the Dev Drive will have after formatting completes.</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    private HRESULT FormatPartitionAsDevDrive(char curDriveLetter, string driveLabel)
    {
        try
        {
            // Since at the time of this call we don't know the unique object ID of our new volume
            // we need to iterate through the volumes that exist to find the one that matches our
            // drive letter. Note: the object ID here is different than what we find in AssignDriveLetterToPartition.
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("root\\Microsoft\\Windows\\Storage", "SELECT * FROM MSFT_Volume");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                var objectId = queryObj["ObjectId"] as string;
                var letter = queryObj["DriveLetter"];
                if (letter is char foundALetter
                    && curDriveLetter == foundALetter &&
                    !string.IsNullOrEmpty(objectId))
                {
                    // Obtain in-parameters for the method
                    ManagementBaseObject inParams =
                        queryObj.GetMethodParameters("Format");

                    // Add the default parameters.
                    inParams["DeveloperVolume"] = true;
                    inParams["FileSystem"] = _fileSytem;
                    inParams["FileSystemLabel"] = driveLabel;
                    inParams["AllocationUnitSize"] = _fourKb;

                    // Execute the method and obtain the return values.
                    ManagementBaseObject outParams =
                        queryObj.InvokeMethod("Format", inParams, new InvokeMethodOptions());

                    var returnValue = (uint)outParams["ReturnValue"];
                    if (returnValue == 0)
                    {
                        return new HRESULT(0);
                    }
                }
            }

            // If we got here that means the returnValue was not successful. We give this a specific error but this will need need
            // to be changed. WMI can return different status and error codes based on the function. The actual returnValue will need
            // to be converted. https://learn.microsoft.com/en-us/windows/win32/wmisdk/wmi-return-codes
            return PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_FUNCTION_FAILED);
        }
        catch (ManagementException e)
        {
            return new HRESULT(e.HResult);
        }
    }
}
