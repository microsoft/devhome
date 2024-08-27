// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;
using Windows.Win32.System.Rpc;

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

    public static void VerifyCaller()
    {
        VerifyCallerIsInTheSameDirectory();
        VerifyCallerIsFromTheSamePackage();
    }

    public static void VerifyCallerIsInTheSameDirectory()
    {
        unsafe
        {
            uint callerPid = 0;
            RPC_STATUS rpcStatus = PInvoke.I_RpcBindingInqLocalClientPID(null, ref callerPid);

            // Unable to figure out our caller
            if (rpcStatus != RPC_STATUS.RPC_S_OK)
            {
                throw new UnauthorizedAccessException();
            }

            Process callerProcess = Process.GetProcessById((int)callerPid);

            FileInfo callerFileInfo = new FileInfo(callerProcess.MainModule?.FileName ?? string.Empty);
            FileInfo serverFileInfo = new FileInfo(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);

            if (!callerFileInfo.Exists || !serverFileInfo.Exists)
            {
                throw new UnauthorizedAccessException();
            }

            if (callerFileInfo.DirectoryName != serverFileInfo.DirectoryName)
            {
                throw new UnauthorizedAccessException();
            }

            // Our caller is in the same directory that we are
        }
    }

    public static void VerifyCallerIsFromTheSamePackage()
    {
        unsafe
        {
            string devHomeServicePackage = Package.Current.Id.FullName;
            HRESULT hr = PInvoke.CoImpersonateClient();
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            Span<char> outputBuffer = new char[10000];
            uint packageFullNameLength = 10000;

            fixed (char* outBufferPointer = outputBuffer)
            {
                var callerPackageName = new PWSTR(outBufferPointer);
                var res = PInvoke.GetPackageFullNameFromToken(identity.AccessToken, ref packageFullNameLength, callerPackageName);
                var callerPackageNameString = new string(callerPackageName);

                if (res != WIN32_ERROR.ERROR_SUCCESS || !devHomeServicePackage.Equals(callerPackageNameString, StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException();
                }
            }

            PInvoke.CoRevertToSelf();

            // We're running with the same package identity
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
