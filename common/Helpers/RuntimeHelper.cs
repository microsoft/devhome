// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
}
