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
public interface IDevDriveStorageOperator
{
    public int CreateAndAttachVhd(string path, ulong size);

    public int GetDiskNumber(string path, out uint diskNumber);

    public Task<int> InitializeDisk(uint diskNumber);

    public Task<int> CreatePartition(uint diskNumber, char driveLetter);

    public Task<int> FormatPartitionAsDevDrive(char driveLetter, string label);
}
