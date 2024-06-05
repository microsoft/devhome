// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using HyperVExtension.Helpers;

namespace HyperVExtension.Models;

public class HyperVVirtualMachineHardDisk
{
    public string? ComputerName { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public Guid VmId { get; set; }

    public string? VMName { get; set; }

    public Guid VMSnapshotId { get; set; }

    public string? VMSnapshotName { get; set; }

    public ulong DiskSizeInBytes { get; set; }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM HardDisk Name: {Name} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM HardDisk VmId: {VmId} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM HardDisk Size : {BytesHelper.ConvertFromBytes(DiskSizeInBytes)} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"VM HardDisk VMSnapshotId : {VMSnapshotId} ");
        return builder.ToString();
    }
}
