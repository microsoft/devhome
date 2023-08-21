// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class DevDriveTaskDefinition : TaskDefinition
{
    private const string _devDrivePath = "--devdrive-path";
    private const string _devDriveSize = "--devdrive-size";
    private const string _devDriveLetter = "--devdrive-letter";
    private const string _devDriveLabel = "--devdrive-label";

    public string VirtDiskPath
    {
        get; set;
    }

    public ulong SizeInBytes
    {
        get; set;
    }

    public char NewDriveLetter
    {
        get; set;
    }

    public string DriveLabel
    {
        get; set;
    }

    public static DevDriveTaskDefinition ReadCliArgument(string[] args, ref int index)
    {
        const int length = 8;
        if (index + length <= args.Length &&
            args[index] == _devDrivePath &&
            args[index + 2] == _devDriveSize &&
            args[index + 4] == _devDriveLetter &&
            args[index + 6] == _devDriveLabel)
        {
            var result = new DevDriveTaskDefinition
            {
                VirtDiskPath = args[index + 1],
                SizeInBytes = ulong.Parse(args[index + 3], CultureInfo.InvariantCulture),
                NewDriveLetter = char.Parse(args[index + 5]),
                DriveLabel = args[index + 7],
            };
            index += length;
            return result;
        }

        return null;
    }

    public override string ToCliArgument()
    {
        return $"{_devDrivePath} \"{VirtDiskPath}\" {_devDriveSize} {SizeInBytes} {_devDriveLetter} {NewDriveLetter} {_devDriveLabel} \"{DriveLabel}\"";
    }
}
