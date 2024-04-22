// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.SetupFlow.Utilities;

namespace DevHome.Common.Models;

/// <summary>
/// Enum to provide callers with the state of the Dev Drive. Dev Drive creation functionality must go through the
/// Dev Drive manager first, who will set back to 'Invalid' if any of the following IDevDrive values are invalid:
/// 1. DriveLetter
/// 2. DriveSizeInBytes
/// 3. DriveLocation
/// 4. DriveLabel
/// 5. DriveSizeRemainingInBytes
/// </summary>
public enum DevDriveState
{
    /// <summary>
    ///  Dev Drive does not exist on system yet and the values it holds have not been
    ///  validated. When in this state we cannot use it to create a Dev Drive on the system.
    /// </summary>
    Invalid,

    /// <summary>
    ///  Dev Drive does not exist on system yet and but the values were valid at the time of validation.
    ///  Dev drive information might become invalid because of user action outside of DevHome. For example,
    ///  making a dev drive via settings. Please re-validate before using.
    /// </summary>
    New,

    /// <summary>
    ///  Dev Drive exists on system. We cannot use it to create a new Dev Drive on the system.
    /// </summary>
    ExistsOnSystem,
}

/// <summary>
///  Dev Drive media type. Matches DiskMediaType in OS code/telemetry.
/// </summary>
public enum DiskMediaType
{
    Unknown = 0,
    HDD = 1,
    SSD = 2,
    Removable = 3,
    VirtualHD = 4,
    SCM = 5,
}

/// <summary>
/// Interface representation for Dev Drives.
/// </summary>
public interface IDevDrive
{
    /// <summary>
    /// Gets or sets the state associated with the Dev Drive.
    /// </summary>
    public DevDriveState State
    {
        get; set;
    }

    /// <summary>
    /// Gets the drive letter for the Dev Drive.
    /// </summary>
    public char DriveLetter
    {
        get;
    }

    /// <summary>
    /// Gets the size for the Dev Drive.
    /// </summary>
    public ulong DriveSizeInBytes
    {
        get;
    }

    /// <summary>
    /// Gets the size remaining for the Dev Drive.
    /// </summary>
    public ulong DriveSizeRemainingInBytes
    {
        get;
    }

    /// <summary>
    /// Gets the file system location of the Dev Drive. This should be a fully qualified folder path.
    /// </summary>
    public string DriveLocation
    {
        get;
    }

    /// <summary>
    /// Gets the drive label that will be used to identify the Dev Drive in the file system.
    /// </summary>
    public string DriveLabel
    {
        get;
    }

    /// <summary>
    /// Gets the app internal ID of the dev drive. This is only used within the app.
    /// This is the ID that the Dev Drive manager will use to determine which requester to view model
    /// association the dev drive belongs to.
    /// </summary>
    public Guid ID
    {
        get;
    }

    /// <summary>
    /// Gets the drive media type.
    /// </summary>
    public DiskMediaType DriveMediaType
    {
        get;
    }

    ByteUnit DriveUnitOfMeasure { get; set; }
}
