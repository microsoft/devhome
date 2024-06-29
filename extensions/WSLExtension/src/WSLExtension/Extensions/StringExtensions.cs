// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace WSLExtension.Extensions;

public static class StringExtensions
{
    public static string FormatArgs(this string source, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, source, args);
    }
}
