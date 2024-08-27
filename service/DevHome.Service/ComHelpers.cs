// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;

namespace DevHome.Service;

internal sealed class ComHelpers
{
    public static void EnableFastCOMRundown()
    {
        CGlobalOptions options = new CGlobalOptions();

        if (options is IGlobalOptions globalOptions)
        {
            globalOptions.SetItem(GLOBALOPT_PROPERTIES.COMGLB_RO_SETTINGS, (uint)GLOBALOPT_RO_FLAGS.COMGLB_FAST_RUNDOWN);
            globalOptions.SetItem(GLOBALOPT_PROPERTIES.COMGLB_EXCEPTION_HANDLING, (uint)GLOBALOPT_EH_VALUES.COMGLB_EXCEPTION_DONOT_HANDLE_ANY);
        }
    }

    public static void InitializeSecurity()
    {
        unsafe
        {
            PInvoke.CoInitializeSecurity((PSECURITY_DESCRIPTOR)null, -1, null, null, RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_NONE, RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IMPERSONATE, null, EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE, null);
        }
    }

    public static void VerifyCallerIsFromTheSamePackage()
    {
        string devHomeServicePackage = Package.Current.Id.FullName;

        HRESULT hr = PInvoke.CoImpersonateClient();

        WindowsIdentity identity = WindowsIdentity.GetCurrent();

        unsafe
        {
            Span<char> outputBuffer = new char[10000];
            uint packageFullNameLength = 10000;

            fixed (char* outBufferPointer = outputBuffer)
            {
                var callerPackageName = new PWSTR(outBufferPointer);
                var res = PInvoke.GetPackageFullNameFromToken(identity.AccessToken, ref packageFullNameLength, callerPackageName);
                var callerPackageNameString = new string(callerPackageName);

                if (res == WIN32_ERROR.ERROR_SUCCESS && devHomeServicePackage.Equals(callerPackageNameString, StringComparison.Ordinal))
                {
                    PInvoke.CoRevertToSelf();
                    return;
                }
            }

            throw new UnauthorizedAccessException();
        }
    }

    [ComImport]
    [Guid("0000034B-0000-0000-C000-000000000046")]
    private class CGlobalOptions;

    [ComImport]
    [Guid("0000015B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGlobalOptions
    {
        void SetItem(GLOBALOPT_PROPERTIES dwProperty, uint dwValue);

        void Query(GLOBALOPT_PROPERTIES dwProperty, out uint pdwValue);
    }
}
