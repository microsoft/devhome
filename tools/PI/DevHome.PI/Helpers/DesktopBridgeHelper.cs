// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.PI.Helpers;

public class DesktopBridgeHelper
{
    public static unsafe bool IsMsixPackagedApp()
    {
        if (IsWindows7OrLower)
        {
            return false;
        }
        else
        {
            uint length = 0;
            var empty = stackalloc char[0];
            WIN32_ERROR result = PInvoke.GetCurrentPackageFullName(ref length, empty);
            var packageName = stackalloc char[(int)length];
            result = PInvoke.GetCurrentPackageFullName(ref length, packageName);
            return result != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
        }
    }

    private static bool IsWindows7OrLower
    {
        get
        {
            var versionMajor = Environment.OSVersion.Version.Major;
            var versionMinor = Environment.OSVersion.Version.Minor;
            var version = versionMajor + ((double)versionMinor / 10);
            return version <= 6.1;
        }
    }
}
