// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.Models;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.DevDrive.Utilities;

/// <summary>
/// Interface for objects that will perform storage operations related to Dev Drives.
/// </summary>
internal interface IDevDriveStorageOperator
{
    /// <summary>
    /// Callers wanting to create a Dev Drive should call this method. The createDevDrive method kicks off
    /// all the operations and methods needed to create a Dev Drive. Note: The implementation of this may
    /// change.
    /// </summary>
    /// <param name="virtDiskPath">The place in the file system the vhdx file will be saved to</param>
    /// <param name="sizeInBytes">The size the drive will be created with</param>
    /// <param name="driveLetter">The drive letter to format the new drive</param>
    /// <param name="driveLabel">The label that will be given to the drive during formatting</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    public HRESULT CreateDevDrive(string virtDiskPath, ulong sizeInBytes, char driveLetter, string driveLabel);

    /// <summary>
    /// Creates the virtual disk and also attaches it so that its lifespan is permanant and survives reboots.
    /// </summary>
    /// <param name="virtDiskPath">The place in the file system the vhdx file will be saved to</param>
    /// <param name="sizeInBytes">The size the drive will be created with</param>
    /// <param name="virtDiskPhysicalPath">The logical representation of the virtual disks physical path on the system</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    public HRESULT CreateAndAttachVhdx(string virtDiskPath, ulong sizeInBytes, out string virtDiskPhysicalPath);

    /// <summary>
    /// Creates a single new Guid Partition table style partition on the virtual disk.
    /// </summary>
    /// <param name="virtDiskPhysicalPath">The logical representation of the virtual disks physical path on the system</param>
    /// <param name="diskNumber">The current number of the new virtual disk</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    public HRESULT CreatePartition(string virtDiskPhysicalPath, out uint diskNumber);

    /// <summary>
    /// Performs the operations needed to update the auto generated drive letter to one that is provided by a caller.
    /// </summary>
    /// <param name="diskNumber">The disk number the method uses to located the correct disk</param>
    /// <param name="newDriveLetter">The new drive letter provided by the caller</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    public HRESULT AssignDriveLetterToPartition(uint diskNumber, char newDriveLetter);

    /// <summary>
    /// Uses WMI to and the storage Api to format the drive as a Dev Drive. Note: the implementation
    /// is subject to change in the future.
    /// </summary>
    /// <param name="driveLetter">The drive letter the method will use when attempting to find a volume and format it</param>
    /// <param name="driveLabel">The new drive label the Dev Drive will have after formatting completes.</param>
    /// <returns>An Hresult that indicates whether the operation succeeded or failed</returns>
    public HRESULT FormatPartitionAsDevDrive(char driveLetter, string driveLabel);

    /// <summary>
    /// Iterates through all the volumes on the users machine to get exiting Dev Drives. Note: The implementation is subject
    /// to change in the future.
    /// </summary>
    /// <returns>
    /// A list of IDevDrives. We use the interface  <see cref="IDevDrive"/> to represent a Dev Drive.
    /// </returns>
    public IEnumerable<IDevDrive> GetAllExistingDevDrives();
}
