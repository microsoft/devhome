// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Common;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

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
    private static readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", nameof(DevDriveUtil)));

    private static readonly ILogger Log = _log.Value;

    public static double MaxSizeForGbComboBox => 64000D;

    public static double MinSizeForGbComboBox => 50D;

    public static double MaxSizeForTbComboBox => 64d;

    public static double MinSizeForTbComboBox => 1D;

    public static int MaxDriveLabelSize => 32;

    public static int MaxDrivePathLength => 32767;

    public static ulong OneKbInBytes => 1ul << 10;

    public static ulong OneMbInBytes => 1ul << 20;

    public static ulong OneGbInBytes => 1ul << 30;

    public static ulong OneTbInBytes => 1ul << 40;

    public static ulong MinDevDriveSizeInBytes => 50ul << 30;

    /// <summary>
    /// Gets the list of the Latin alphabet. Windows uses the Latin letters as its drive letters.
    /// </summary>
    public static IList<char> DriveLetterCharArray => new List<char>("CDEFGHIJKLMNOPQRSTUVWXYZ");

    public static readonly List<char> InvalidCharactersNotInGetInvalidPathChars = new() { '*', '?', '\"', '<', '>', '|' };

    private enum DEVELOPER_DRIVE_ENABLEMENT_STATE
    {
        DeveloperDriveEnablementStateError = 0,
        DeveloperDriveEnabled = 1,
        DeveloperDriveDisabledBySystemPolicy = 2,
        DeveloperDriveDisabledByGroupPolicy = 3,
    }

    // Not available in CsWin32, so we need to add this ourselves.
    [DllImport("api-ms-win-core-sysinfo-l1-2-6.dll")]
    private static extern DEVELOPER_DRIVE_ENABLEMENT_STATE GetDeveloperDriveEnablementState();

    private static readonly BOOL IsDevDriveFeaturePresent = PInvoke.IsApiSetImplemented("api-ms-win-core-sysinfo-l1-2-6");

    /// <summary>
    /// Gets a value indicating whether the system has the ability to create Dev Drives
    /// and whether the ability is enabled. Windows 10 or below machines will not have this ability.
    /// </summary>
    /// <returns>
    /// Returns true only if the Dev Drive feature is present and enabled on the machine.
    /// </returns>
    public static bool IsDevDriveFeatureEnabled
    {
        get
        {
            try
            {
                if (!IsDevDriveFeaturePresent)
                {
                    return false;
                }

                return GetDeveloperDriveEnablementState() == DEVELOPER_DRIVE_ENABLEMENT_STATE.DeveloperDriveEnabled;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to query for Dev Drive enablement: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Given a byte unit which is either GB or TB converts the value parameter to bytes.
    /// Since this is just for DevDrives the minimum size is known of value
    /// is 50 (50 gb) and its max size should be 64 (64 Tb) which is
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

        // Deal with decimal portion by getting the floor of the value in KB + half. This is to allow us to round up.
        // should the decimal portion be greater than 0.5. Also helps keep the value a multiple of 512 when we multiply
        // by either 1 MB or 1 GB further down. This is crucial for the Version2.MaximumSize parameter in CREATE_VIRTUAL_DISK_PARAMETERS that wis used to
        // create the virtual disk. https://learn.microsoft.com/windows/win32/api/virtdisk/ns-virtdisk-create_virtual_disk_parameters
        var floorOfValueInKB = (ulong)Math.Floor((value * OneKbInBytes) + 0.5);
        if (unitIsTb)
        {
            return floorOfValueInKB * OneGbInBytes;
        }

        return floorOfValueInKB * OneMbInBytes;
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

        // If value over 1 Tb then convert this with terabytes in mind.
        var tempValue = value / OneGbInBytes;
        if (value.CompareTo(OneTbInBytes) >= 0)
        {
            tempValue = value / OneTbInBytes;
            return (tempValue, ByteUnit.TB);
        }

        return (tempValue, ByteUnit.GB);
    }

    /// <summary>
    /// Checks whether a filename or a path is invalid.
    /// </summary>
    /// <param name="type">The type of invalid characters we need to get. Either for a path or filename</param>
    /// <param name="fileNameOrPath">A string that is either a path or a filename</param>
    /// <returns>Bool stating whether string is invalid based on the type passed in</returns>
    public static bool IsInvalidFileNameOrPath(InvalidCharactersKind type, string fileNameOrPath)
    {
        var invalidChars = GetInvalidCharacters(type);
        var oppositeSlash = fileNameOrPath.Contains('\\') ? '/' : '\\';
        invalidChars.Add(oppositeSlash);
        return fileNameOrPath.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Gets the set of invalid characters based on the passed in type. Type can be either a path or filename.
    /// </summary>
    /// <param name="type">The type of invalid characters to get. Either for a path or filename</param>
    /// <returns>Set of invalid characters based on the type passed in</returns>
    private static HashSet<char> GetInvalidCharacters(InvalidCharactersKind type)
    {
        List<char> invalidFileChars;
        if (type == InvalidCharactersKind.Path)
        {
            invalidFileChars = Path.GetInvalidPathChars().ToList();
            invalidFileChars.AddRange(InvalidCharactersNotInGetInvalidPathChars);
        }
        else
        {
            invalidFileChars = Path.GetInvalidFileNameChars().ToList();
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
    /// implementation which is in English.
    /// </returns>
    public static string ConvertBytesToString(ulong sizeInBytes)
    {
        unsafe
        {
            // 15 characters + null terminator.
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

    public static bool IsCharValidDriveLetter(char? letter)
    {
        if (letter.HasValue)
        {
            return DriveLetterCharArray.Contains(char.ToUpperInvariant(letter.Value));
        }

        return false;
    }

    private static string FormatExceptionString(ByteUnit unit, double value, double minSize, double maxSize)
    {
        return $"When unit set to {unit}, value ({value}) must be greater than or equal to {minSize} and less than or equal to {maxSize}";
    }
}
