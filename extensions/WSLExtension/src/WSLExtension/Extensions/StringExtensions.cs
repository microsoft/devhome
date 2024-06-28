// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLExtension.Extensions;

public static class StringExtensions
{
    public static string FormatArgs(this string source, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, source, args);
    }
}
