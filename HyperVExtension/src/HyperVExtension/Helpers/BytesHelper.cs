// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace HyperVExtension.Helpers;

public static class BytesHelper
{
    public const decimal OneKbInBytes = 1ul << 10;

    public const decimal OneMbInBytes = 1ul << 20;

    public const decimal OneGbInBytes = 1ul << 30;

    public const decimal OneTbInBytes = 1ul << 40;

    /// <summary>
    /// Converts bytes represented by a long value to its human readable string
    /// equivalent in either megabytes, gigabytes or terabytes.
    /// Note: this is only for internal use and is not localized.
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

        return $"{(size / OneMbInBytes).ToString("F", CultureInfo.InvariantCulture)} MB";
    }
}
