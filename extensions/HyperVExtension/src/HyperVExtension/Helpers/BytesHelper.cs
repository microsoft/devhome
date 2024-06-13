// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace HyperVExtension.Helpers;

public static class BytesHelper
{
    private const int ByteToStringBufferSize = 16;

    public const decimal OneKbInBytes = 1ul << 10;

    public const decimal OneMbInBytes = 1ul << 20;

    public const decimal OneGbInBytes = 1ul << 30;

    public const decimal OneTbInBytes = 1ul << 40;

    /// <summary>
    /// Converts bytes represented by a long value to its human readable string
    /// equivalent in either megabytes, gigabytes or terabytes.
    /// </summary>
    public static string ConvertFromBytes(ulong size)
    {
        if (size >= OneTbInBytes)
        {
            return $"{(size / OneTbInBytes).ToString("F", CultureInfo.InvariantCulture)} TB";
        }
        else if (size >= OneGbInBytes)
        {
            return $"{(size / OneGbInBytes).ToString("F", CultureInfo.InvariantCulture)} GB";
        }
        else if (size >= OneMbInBytes)
        {
            return $"{(size / OneMbInBytes).ToString("F", CultureInfo.InvariantCulture)} MB";
        }

        return $"{(size / OneKbInBytes).ToString("F", CultureInfo.InvariantCulture)} KB";
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
            var buffer = new string(' ', ByteToStringBufferSize);
            fixed (char* tempPath = buffer)
            {
                var result =
                    PInvoke.StrFormatByteSizeEx(
                        sizeInBytes,
                        SFBS_FLAGS.SFBS_FLAGS_TRUNCATE_UNDISPLAYED_DECIMAL_DIGITS,
                        tempPath,
                        ByteToStringBufferSize);
                if (result != HRESULT.S_OK)
                {
                    // Fallback to using non localized version which is in english.
                    return ConvertFromBytes(sizeInBytes);
                }
                else
                {
                    return buffer.Trim().Trim('\0');
                }
            }
        }
    }
}
