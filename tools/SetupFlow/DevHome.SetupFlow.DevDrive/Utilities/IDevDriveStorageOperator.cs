// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.DevDrive.Utilities;

/// <summary>
/// Interface for objects that will perform storage operations related to Dev Drives.
/// </summary>
internal interface IDevDriveStorageOperator
{
    public Task<HRESULT> CreateDevDrive(string virtDiskPath, ulong sizeInBytes, char driveLetter, string driveLabel);

    public HRESULT CreateAndAttachVhdx(string virtDiskPath, ulong sizeInBytes, out string virtDiskPhysicalPath);

    public HRESULT CreatePartition(string virtDiskPhysicalPath, out uint diskNumber);

    public HRESULT AssignDriveLetterToPartition(uint diskNumber, char newDriveLetter);

    public Task<HRESULT> FormatPartitionAsDevDrive(char driveLetter, string driveLabel);
}
