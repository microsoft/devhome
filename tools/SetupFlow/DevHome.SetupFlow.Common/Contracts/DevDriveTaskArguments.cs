// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;

/// <summary>
/// Class representing a dev drive task arguments
/// </summary>
public sealed class DevDriveTaskArguments
{
    private const string _devDrivePath = "--devdrive-path";
    private const string _devDriveSize = "--devdrive-size";
    private const string _devDriveLetter = "--devdrive-letter";
    private const string _devDriveLabel = "--devdrive-label";

    /// <summary>
    /// Gets or sets the drive's virtual disk path
    /// </summary>
    public string VirtDiskPath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive's size in bytes
    /// </summary>
    public ulong SizeInBytes
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive's letter
    /// </summary>
    public char NewDriveLetter
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive's label
    /// </summary>
    public string DriveLabel
    {
        get; set;
    }

    /// <summary>
    /// Try to read and parse argument list into an object.
    /// </summary>
    /// <param name="argumentList">Argument list</param>
    /// <param name="index">Index to start reading arguments from</param>
    /// <param name="result">Output object</param>
    /// <returns>True if reading arguments succeeded. False otherwise.</returns>
    public static bool TryReadArguments(IList<string> argumentList, ref int index, out DevDriveTaskArguments result)
    {
        result = null;

        // --devdrive-path <path>      --devdrive-size <size>      --devdrive-letter <letter>    --devdrive-label <label>
        // [index]         [index + 1] [index + 2]     [index + 3] [index + 4]       [index + 5] [index + 6]      [index + 7]
        const int taskArgListCount = 8;
        if (index + taskArgListCount <= argumentList.Count &&
            argumentList[index] == _devDrivePath &&
            argumentList[index + 2] == _devDriveSize &&
            argumentList[index + 4] == _devDriveLetter &&
            argumentList[index + 6] == _devDriveLabel)
        {
            if (!ulong.TryParse(argumentList[index + 3], out var sizeInBytes) ||
                !char.TryParse(argumentList[index + 5], out var letter))
            {
                return false;
            }

            result = new DevDriveTaskArguments
            {
                VirtDiskPath = argumentList[index + 1],
                SizeInBytes = sizeInBytes,
                NewDriveLetter = letter,
                DriveLabel = argumentList[index + 7],
            };
            index += taskArgListCount;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create a list of arguments from this object.
    /// </summary>
    /// <returns>List of argument strings from this object</returns>
    public List<string> ToArgumentList()
    {
        return new ()
        {
            _devDrivePath, VirtDiskPath,            // --devdrive-path <path>
            _devDriveSize, $"{SizeInBytes}",        // --devdrive-size <size>
            _devDriveLetter, $"{NewDriveLetter}",   // --devdrive-letter <letter>
            _devDriveLabel, DriveLabel,             // --devdrive-label <label>
        };
    }
}
