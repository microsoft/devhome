// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using static DevHome.Common.Helpers.RuntimeHelper;

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

    // Determine whether the current process is running elevated in a split token session
    // will not return true if UAC is disabled and the user is running as administrator by default
    public static unsafe bool IsCurrentProcessRunningElevated()
    {
        HANDLE tokenHandle;
        if (!PInvoke.OpenProcessToken(PInvoke.GetCurrentProcess(), TOKEN_ACCESS_MASK.TOKEN_QUERY, &tokenHandle))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            TOKEN_ELEVATION_TYPE elevationType;
            uint elevationTypeSize = (uint)Unsafe.SizeOf<TOKEN_ELEVATION_TYPE>();
            uint returnLength;

            if (!PInvoke.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, &elevationType, elevationTypeSize, &returnLength))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
        }
        finally
        {
            PInvoke.CloseHandle(tokenHandle);
        }
    }

    public static void VerifyCurrentProcessRunningAsAdmin()
    {
        if (!IsCurrentProcessRunningAsAdmin())
        {
            throw new UnauthorizedAccessException("This operation requires elevated privileges.");
        }
    }
}
