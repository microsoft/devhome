// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace DevHome.Common.Helpers;

public static class RuntimeHelper
{
    public static unsafe bool IsMSIX
    {
        get
        {
            const int APPMODEL_ERROR_NO_PACKAGE = 15700;

            uint length;

            return GetCurrentPackageFullName(&length, null) != APPMODEL_ERROR_NO_PACKAGE;

            // See: https://learn.microsoft.com/windows/win32/api/appmodel/nf-appmodel-getcurrentpackagefullname
            [DllImport("kernel32", ExactSpelling = true)]
            static extern int GetCurrentPackageFullName(uint* packageFullNameLength, ushort* packageFullName);
        }
    }
}
