// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using COMRegistration;

namespace DevHome.Service;

public sealed class WindowsBackgroundService(
    ProcessNotificationService jokeService,
    ILogger<WindowsBackgroundService> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RegisterClass(new Guid("1F98F450-C163-4A99-B257-E1E6CB3E1C57"));
        EnableFastCOMRundown();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string joke = jokeService.GetJokeInternal();
                logger.LogWarning("{Joke}", joke);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    private readonly List<int> _registrationCookies = new();

    public void RegisterClass(Guid clsid)
    {
        Trace.WriteLine($"Registering class object:");
        Trace.Indent();
        Trace.WriteLine($"CLSID: {clsid:B}");

        int cookie;
        int hr = Ole32.CoRegisterClassObject(ref clsid, new BasicClassFactory<ProcessNotificationService>(), Ole32.CLSCTX_LOCAL_SERVER, Ole32.REGCLS_MULTIPLEUSE | Ole32.REGCLS_SUSPENDED, out cookie);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        _registrationCookies.Add(cookie);
        Trace.WriteLine($"Cookie: {cookie}");
        Trace.Unindent();

        hr = Ole32.CoResumeClassObjects();
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    private sealed class Ole32
    {
        // https://docs.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx
#pragma warning disable SA1310 // Field names should not contain underscore
        public const int CLSCTX_LOCAL_SERVER = 0x4;
#pragma warning restore SA1310 // Field names should not contain underscore

        // https://docs.microsoft.com/windows/win32/api/combaseapi/ne-combaseapi-regcls
#pragma warning disable SA1310 // Field names should not contain underscore
        public const int REGCLS_MULTIPLEUSE = 1;
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning disable SA1310 // Field names should not contain underscore
        public const int REGCLS_SUSPENDED = 4;
#pragma warning restore SA1310 // Field names should not contain underscore

        // https://docs.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-coregisterclassobject
        [DllImport(nameof(Ole32))]
        public static extern int CoRegisterClassObject(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] object obj, int context, int flags, out int register);

        // https://docs.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-coresumeclassobjects
        [DllImport(nameof(Ole32))]
        public static extern int CoResumeClassObjects();

        // https://docs.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-corevokeclassobject
        [DllImport(nameof(Ole32))]
        public static extern int CoRevokeClassObject(int register);
    }

    private void EnableFastCOMRundown()
    {
        CGlobalOptions options = new CGlobalOptions();

        if (options is IGlobalOptions globalOptions)
        {
            globalOptions.SetItem(COMGlobalOptions.COMGLB_RO_SETTINGS, (uint)COM_RO_FLAGS.COMGLB_FAST_RUNDOWN);
            globalOptions.SetItem(COMGlobalOptions.COMGLB_EXCEPTION_HANDLING, (uint)COMExeceptionHandling.COMGLB_EXCEPTION_DONOT_HANDLE_ANY);
        }
    }

    [ComImport]
    [Guid("0000034B-0000-0000-C000-000000000046")]
    public class CGlobalOptions;

    [ComImport]
    [Guid("0000015B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IGlobalOptions
    {
        void SetItem(COMGlobalOptions dwProperty, uint dwValue);

        void Query(COMGlobalOptions dwProperty, out uint pdwValue);
    }

    public enum COMGlobalOptions
    {
        COMGLB_EXCEPTION_HANDLING = 1,
        COMGLB_APPID = 2,
        COMGLB_RPC_THREADPOOL_SETTING = 3,
        COMGLB_RO_SETTINGS = 4,
        COMGLB_UNMARSHALING_POLICY = 5,
    }

    public enum COMExeceptionHandling
    {
        COMGLB_EXCEPTION_HANDLE = 0,
        COMGLB_EXCEPTION_DONOT_HANDLE_FATAL = 1,
        COMGLB_EXCEPTION_DONOT_HANDLE = COMGLB_EXCEPTION_DONOT_HANDLE_FATAL, // Alias for compatibility
        COMGLB_EXCEPTION_DONOT_HANDLE_ANY = 2,
    }

    public enum COM_RO_FLAGS
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
