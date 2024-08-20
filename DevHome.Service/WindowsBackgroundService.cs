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
}
