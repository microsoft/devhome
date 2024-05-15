// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Models;
using DevHome.SetupFlow.Utilities;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Model class representation for Dev Drives.
/// </summary>
public class DevDrive : IDevDrive
{
    public DevDrive(
        char driveLetter,
        ulong driveSizeInBytes,
        ulong driveSizeRemainingInBytes,
        ByteUnit driveUnitOfMeasure,
        string driveLocation,
        string driveLabel,
        DevDriveState state,
        Guid id)
        : this()
    {
        DriveLetter = driveLetter;
        DriveSizeInBytes = driveSizeInBytes;
        DriveSizeRemainingInBytes = driveSizeRemainingInBytes;
        DriveUnitOfMeasure = driveUnitOfMeasure;
        DriveLocation = driveLocation;
        DriveLabel = driveLabel;
        State = state;
        ID = id;
    }

    public DevDrive(DevDrive devDrive)
    {
        DriveLetter = devDrive.DriveLetter;
        DriveSizeInBytes = devDrive.DriveSizeInBytes;
        DriveSizeRemainingInBytes = devDrive.DriveSizeRemainingInBytes;
        DriveUnitOfMeasure = devDrive.DriveUnitOfMeasure;
        DriveLocation = devDrive.DriveLocation;
        DriveLabel = devDrive.DriveLabel;
        State = devDrive.State;
        ID = devDrive.ID;
        DriveMediaType = devDrive.DriveMediaType;
    }

    public DevDrive(IDevDrive devDrive)
    {
        DriveLetter = devDrive.DriveLetter;
        DriveSizeInBytes = devDrive.DriveSizeInBytes;
        DriveSizeRemainingInBytes = devDrive.DriveSizeRemainingInBytes;
        DriveLocation = devDrive.DriveLocation;
        DriveLabel = devDrive.DriveLabel;
        State = devDrive.State;
        ID = devDrive.ID;
        DriveMediaType = devDrive.DriveMediaType;

        if (DriveSizeInBytes >= DevDriveUtil.OneTbInBytes)
        {
            DriveUnitOfMeasure = ByteUnit.TB;
        }
        else
        {
            DriveUnitOfMeasure = ByteUnit.GB;
        }
    }

    public DevDrive()
    {
        ID = Guid.NewGuid();
        DriveMediaType = DiskMediaType.VirtualHD; // Other types are not supported/used at the moment.
    }

    /// <summary>
    /// Gets or sets the state associated with the Dev Drive. Default to the default value of Invalid.
    /// </summary>
    public DevDriveState State
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive letter for the Dev Drive.
    /// </summary>
    public char DriveLetter
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the size for the Dev Drive. This size is represented in base2 where one kilobyte is
    /// 1024 bytes.
    /// </summary>
    public ulong DriveSizeInBytes
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the size remaining for the Dev Drive. This size is represented in base2 where one kilobyte is
    /// 1024 bytes.
    /// </summary>
    public ulong DriveSizeRemainingInBytes
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the file system location of the Dev Drive. This should be a fully qualified folder path.
    /// </summary>
    public string DriveLocation
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive label that will be used to identify the Dev Drive in the file system.
    /// </summary>
    public string DriveLabel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the byte unit of measure for the Dev Drive.
    /// </summary>
    public ByteUnit DriveUnitOfMeasure
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the app internal ID of the dev drive. This is only used within the app.
    /// </summary>
    public Guid ID
    {
        get; set;
    }

    /// <summary>
    /// Gets the drive media type.
    /// </summary>
    public DiskMediaType DriveMediaType
    {
        get;
    }
}
