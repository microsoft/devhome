// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
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
            globalOptions.SetItem(COMGlobalOptions.COMGLB_RO_SETTINGS, (uint)COM_RO_FLAGS.COMGLB_FAST_RUNDOWN);
            globalOptions.SetItem(COMGlobalOptions.COMGLB_EXCEPTION_HANDLING, (uint)COMExeceptionHandling.COMGLB_EXCEPTION_DONOT_HANDLE_ANY);
        }
    }

    public static void InitializeSecurity()
    {
        unsafe
        {
            PInvoke.CoInitializeSecurity((PSECURITY_DESCRIPTOR)null, -1, null, null, RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_NONE, RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IMPERSONATE, null, EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE, null);
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
        void SetItem(COMGlobalOptions dwProperty, uint dwValue);

        void Query(COMGlobalOptions dwProperty, out uint pdwValue);
    }

    private enum COMGlobalOptions
    {
        COMGLB_EXCEPTION_HANDLING = 1,
        COMGLB_APPID = 2,
        COMGLB_RPC_THREADPOOL_SETTING = 3,
        COMGLB_RO_SETTINGS = 4,
        COMGLB_UNMARSHALING_POLICY = 5,
    }

    private enum COMExeceptionHandling
    {
        COMGLB_EXCEPTION_HANDLE = 0,
        COMGLB_EXCEPTION_DONOT_HANDLE_FATAL = 1,
        COMGLB_EXCEPTION_DONOT_HANDLE = COMGLB_EXCEPTION_DONOT_HANDLE_FATAL, // Alias for compatibility
        COMGLB_EXCEPTION_DONOT_HANDLE_ANY = 2,
    }

    private enum COM_RO_FLAGS
    {
        // Remove touch messages from the message queue in the STA modal loop.
        COMGLB_STA_MODALLOOP_REMOVE_TOUCH_MESSAGES = 0x1,

        // Flags that control the behavior of input message removal in
        // the STA modal loop when the thread's message queue is attached.
        COMGLB_STA_MODALLOOP_SHARED_QUEUE_REMOVE_INPUT_MESSAGES = 0x2,
        COMGLB_STA_MODALLOOP_SHARED_QUEUE_DONOT_REMOVE_INPUT_MESSAGES = 0x4,

        // Flag to opt-in to the fast rundown option.
        COMGLB_FAST_RUNDOWN = 0x8,

        // Reserved
        COMGLB_RESERVED1 = 0x10,
        COMGLB_RESERVED2 = 0x20,
        COMGLB_RESERVED3 = 0x40,

        // Flag to opt-in to pointer message re-ordering when
        // queues are attached.
        COMGLB_STA_MODALLOOP_SHARED_QUEUE_REORDER_POINTER_MESSAGES = 0x80,

        COMGLB_RESERVED4 = 0x100,
        COMGLB_RESERVED5 = 0x200,
        COMGLB_RESERVED6 = 0x400,
    }
}
