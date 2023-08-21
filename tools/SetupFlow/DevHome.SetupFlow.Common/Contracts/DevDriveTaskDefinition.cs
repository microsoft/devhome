// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class DevDriveTaskDefinition : ITaskDefinition
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

    public static bool TryReadArguments(IList<string> tasksDefinitionArgumentList, ref int index, out DevDriveTaskDefinition result)
    {
        result = null;
        const int taskArgListCount = 8;
        if (index + taskArgListCount <= tasksDefinitionArgumentList.Count &&
            tasksDefinitionArgumentList[index] == _devDrivePath &&
            tasksDefinitionArgumentList[index + 2] == _devDriveSize &&
            tasksDefinitionArgumentList[index + 4] == _devDriveLetter &&
            tasksDefinitionArgumentList[index + 6] == _devDriveLabel)
        {
            if (!ulong.TryParse(tasksDefinitionArgumentList[index + 3], out var sizeInBytes) ||
                !char.TryParse(tasksDefinitionArgumentList[index + 5], out var letter))
            {
                return false;
            }

            result = new DevDriveTaskDefinition
            {
                VirtDiskPath = tasksDefinitionArgumentList[index + 1],
                SizeInBytes = sizeInBytes,
                NewDriveLetter = letter,
                DriveLabel = tasksDefinitionArgumentList[index + 7],
            };
            index += taskArgListCount;
            return true;
        }

        return false;
    }

    public List<string> ToArgumentList()
    {
        return new ()
        {
            _devDrivePath, VirtDiskPath,
            _devDriveSize, $"{SizeInBytes}",
            _devDriveLetter, $"{NewDriveLetter}",
            _devDriveLabel, DriveLabel,
        };
    }
}
