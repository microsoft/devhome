// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using DevHome.Common.Models;
using DevHome.SetupFlow.DevDrive.Models;
using ToolKitHelpers = CommunityToolkit.WinUI.Helpers;

namespace DevHome.SetupFlow.DevDrive.Utilities;

/// <summary>
/// Enum Representation for a multiple of bytes.
/// Only Gigabytes and Terabytes are supported.
/// </summary>
public enum ByteUnit
{
    GB,
    TB,
}

/// <summary>
/// Utility class to perform actions related to Dev Drives.
/// </summary>
public static class DevDriveUtil
{
    /// <summary>
    /// Gets a value indicating whether the system has the ability to create Dev Drives
    /// and whether the ability is enabled. Win10 machines will not have this ability.
    /// </summary>
    /// <returns>
    /// Returns true if Dev Drive creation functionality is present on the machine
    /// </returns>
    public static bool IsDevDriveFeatureEnabled
    {
        get
        {
            var osVersion = ToolKitHelpers.SystemInformation.Instance.OperatingSystemVersion;
            if (osVersion.Major == 10 && osVersion.Minor == 0 && osVersion.Build < 22000)
            {
                // Win 10
                return false;
            }

            if (osVersion.Major == 10 && osVersion.Minor == 0 && osVersion.Build >= 25309)
            {
                // Canary Insiders dev channel use the 25000 series numbering. Feature is enabled there.
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Given a byte unit which is either GB or TB converts the value to bytes.
    /// </summary>
    /// <returns>
    /// the total size of the value in bytes.
    /// </returns>
    public static ulong ConvertToBytes(ulong value, ByteUnit unit) => throw new NotImplementedException();

    /// <summary>
    /// Validates the values inside the dev drive against Dev Drive requirements.
    /// </summary>
    /// <returns>A bool to state whether the data in the IDevDrive is valid or not</returns>
    public static bool ValidateDevDrive(IDevDrive devDrive) => throw new NotImplementedException();

    /// <summary>
    /// Gets All available drive letters on the system.
    /// </summary>
    /// <returns>A list of drive letters currently not in use</returns>
    public static IList<char> GetAvailableDriveLetters() => throw new NotImplementedException();
}
