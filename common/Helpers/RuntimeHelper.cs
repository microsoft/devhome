// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.Common.Helpers;

public static class RuntimeHelper
{
    public static bool IsMSIX
    {
        get
        {
            uint length = 0;

            return PInvoke.GetCurrentPackageFullName(ref length, null) != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
        }
    }

    public static bool IsOnWindows11
    {
        get
        {
            var version = Environment.OSVersion.Version;
            return version.Major >= 10 && version.Build >= 22000;
        }
    }

    public static bool IsCurrentProcessRunningAsAdmin()
    {
        var identity = WindowsIdentity.GetCurrent();
        return identity.Owner?.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ?? false;
    }

    public static void VerifyCurrentProcessRunningAsAdmin()
    {
        if (!IsCurrentProcessRunningAsAdmin())
        {
            throw new UnauthorizedAccessException("This operation requires elevated privileges.");
        }
    }
}
