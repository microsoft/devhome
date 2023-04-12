// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Common;
using DevHome.SetupFlow.Helpers;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using ToolKitHelpers = CommunityToolkit.WinUI.Helpers;

namespace DevHome.SetupFlow.Utilities;

/// <summary>
/// Enum Representation for a multiple of bytes.
/// Only Gigabytes and Terabytes are supported.
/// </summary>
public enum ByteUnit
{
    GB,
    TB,
}

public enum InvalidCharactersKind
{
    Path,
    FileName,
}

/// <summary>
/// Utility class to perform actions related to Dev Drives.
/// </summary>
public static class DevDriveUtil
{
    public static double MaxSizeForGbComboBox => 64000D;

    public static double MinSizeForGbComboBox => 50D;

    public static double MaxSizeForTbComboBox => 64d;

    public static double MinSizeForTbComboBox => 0.5D;

    public static int MaxDriveLabelSize => 32;

    public static int MaxDrivePathLength => 32767;

    public static ulong OneGbInBytes => 1ul << 30;

    public static ulong OneTbInBytes => 1ul << 40;

    public static ulong MinDevDriveSizeInBytes => 50ul << 30;

    /// <summary>
    /// Gets the list of the latin alphabet. Windows uses the latin letters as its drive letters.
    /// </summary>
    public static IList<char> DriveLetterCharArray => new List<char>("CDEFGHIJKLMNOPQRSTUVWXYZ");

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
                Log.Logger?.ReportInfo(Log.Component.DevDrive, "Dev Drive feature is not available on Win10");
                return false;
            }

            if (osVersion.Major == 10 && osVersion.Minor == 0 && osVersion.Build >= 25309)
            {
                // Canary Insiders dev channel use the 25000 series numbering. Feature is enabled there.
                Log.Logger?.ReportInfo(Log.Component.DevDrive, "Dev Drive feature is not available on older Win11 builds");
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Given a byte unit which is either GB or TB converts the value paramter to bytes.
    /// Since this is just for DevDrives we know that the minimum size
    /// that the value paramter should be is 50 (50 gb) and its max size should be 64  (64 Tb) which is
    /// the max size that a vhdx file can be.
    /// </summary>
    /// <param name="value">Dev Drive object</param>
    /// <returns>
    /// the total size of the value in bytes.
    /// </returns>
    public static ulong ConvertToBytes(double value, ByteUnit unit)
    {
        var unitIsTb = unit == ByteUnit.TB;
        if (!IsValidSize(value, unit))
        {
            var minSize = MinSizeForGbComboBox;
            var maxSize = MaxSizeForGbComboBox;
            if (unitIsTb)
            {
                minSize = MinSizeForTbComboBox;
                maxSize = MaxSizeForTbComboBox;
            }

            throw new ArgumentException(FormatExceptionString(unit, value, minSize, maxSize));
        }

        if (unitIsTb && value.CompareTo(1.0d) < 0)
        {
            // Switch this to Gb, e.g if value is 0.05 we make this 50 by multiplying by 1000.
            unit = ByteUnit.GB;
            value *= 1000d;
        }
        else if (!unitIsTb && value.CompareTo(1000.0d) >= 0)
        {
            // Switch this to Tb, e.g if value is 4322 we make this 4.322 by dividing by 1000.
            unit = ByteUnit.TB;
            value /= 1000d;
        }

        // Since we only care about Gb and Tb here we can use the power function
        // to get our value in bytes. Where 1024.0^3 gives us 1 Gb and 1024.0^4 gives
        // us 1 Tb.
        if (unit == ByteUnit.TB)
        {
            return (ulong)(value * OneTbInBytes);
        }

        return (ulong)(value * OneGbInBytes);
    }

    /// <summary>
    /// This method does not test if the value is valid in bytes. It is used for the Dev Drive window size combo box.
    /// It simply checks if the value is within the given human readable range. E.g for gigiabytes it checks if value is below 50 or over 64000.
    /// For the terabytes case below 0.05 or over 64. Minimum Dev Drive size is 50 GB and maximum file size for vhdx is 64 Tb.
    /// </summary>
    /// <returns> A bool that says whether the value is within the required bounds for Dev Drives</returns>
    public static bool IsValidSize(double value, ByteUnit unit)
    {
        bool valueIsGreaterOrEqualToMin;
        bool valueIsLessThanOrEqualToMax;
        if (unit == ByteUnit.TB)
        {
            valueIsGreaterOrEqualToMin = value.CompareTo(MinSizeForTbComboBox) >= 0;
            valueIsLessThanOrEqualToMax = value.CompareTo(MaxSizeForTbComboBox) <= 0;
        }
        else
        {
            valueIsGreaterOrEqualToMin = value.CompareTo(MinSizeForGbComboBox) >= 0;
            valueIsLessThanOrEqualToMax = value.CompareTo(MaxSizeForGbComboBox) <= 0;
        }

        return valueIsGreaterOrEqualToMin && valueIsLessThanOrEqualToMax;
    }

    /// <summary>
    /// Given a value in bytes we converts it to gigabytes or terabytes. Note this does not format
    /// the values, just gets the numerical value.
    /// </summary>
    /// <param name="value">value in bytes to be converted</param>
    /// <returns>
    /// The double representation of the newly converted value and its byte unit to tell the caller if its
    /// in gigabytes or terabytes.
    /// </returns>
    public static (double, ByteUnit) ConvertFromBytes(double value)
    {
        if (value.CompareTo(0.0d) <= 0)
        {
            throw new ArgumentException("Invalid size in bytes, value must be greater than 0");
        }

        // If value over 1 Tb then we know that we should convert this with terabytes in mind.
        var tempValue = value / OneGbInBytes;
        if (value.CompareTo(OneTbInBytes) >= 0)
        {
            tempValue = value / OneTbInBytes;
            return (tempValue, ByteUnit.TB);
        }

        return (tempValue, ByteUnit.GB);
    }

    public static bool IsInvalidFileNameOrPath(InvalidCharactersKind type, string fileNameOrPath)
    {
        var invalidChars = GetInvalidCharacters(type);
        return fileNameOrPath.Any(c => invalidChars.Contains(c));
    }

    private static ISet<char> GetInvalidCharacters(InvalidCharactersKind type)
    {
        char[] invalidFileChars;
        if (type == InvalidCharactersKind.Path)
        {
            invalidFileChars = Path.GetInvalidPathChars();
        }
        else
        {
            invalidFileChars = Path.GetInvalidFileNameChars();
        }

        var returnSet = new HashSet<char>();
        foreach (var character in invalidFileChars)
        {
            if (!char.IsWhiteSpace(character))
            {
                returnSet.Add(character);
            }
        }

        return returnSet;
    }

    /// <summary>
    /// Converts from a ulong amount of bytes to a localized string representation of that byte size in gigabytes.
    /// Relies on StrFormatByteSizeEx to convert to localized.
    /// </summary>
    /// <returns>
    /// If succeeds internally return localized size in gigabytes, otherwise falls back to community toolkit
    /// implementation which is in english.
    /// </returns>
    public static string ConvertBytesToString(ulong sizeInBytes)
    {
        unsafe
        {
            // We only need 15 characters + null terminator.
            var buffer = new string(' ', 16);
            fixed (char* tempPath = buffer)
            {
                var result =
                    PInvoke.StrFormatByteSizeEx(
                        sizeInBytes,
                        SFBS_FLAGS.SFBS_FLAGS_TRUNCATE_UNDISPLAYED_DECIMAL_DIGITS,
                        tempPath,
                        PInvoke.MAX_PATH);
                if (result != 0)
                {
                    // fallback to using community toolkit which shows this unlocalized. In the form of 50 GB, 40 TB etc.
                    return Converters.ToFileSizeString((long)sizeInBytes);
                }
                else
                {
                    return buffer.Trim();
                }
            }
        }
    }

    private static string FormatExceptionString(ByteUnit unit, double value, double minSize, double maxSize)
    {
        return $"When unit set to {unit}, value ({value}) must be greater than or equal to {minSize} and less than or equal to {maxSize}";
    }
}
