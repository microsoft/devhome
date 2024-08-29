// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using COMRegistration;
using DevHome.Service.Runtime;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace DevHome.Service;

public sealed class WindowsBackgroundService() : BackgroundService
{
    private static readonly ManualResetEventSlim _stopEvent = new(false);

    public static void Stop()
    {
        _stopEvent.Set();
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
#if CANARY_BUILD
        RegisterClass<DevHomeService>(new Guid("0A920C6E-2569-44D1-A6E4-CE9FA44CD2A7"));
#elif STABLE_BUILD
        RegisterClass<DevHomeService>(new Guid("E8D40232-20A1-4F3B-9C0C-AAA6538698C6"));
#else
        RegisterClass<DevHomeService>(new Guid("1F98F450-C163-4A99-B257-E1E6CB3E1C57"));
#endif

        try
        {
            await Task.Run(() => _stopEvent.Wait(stoppingToken), stoppingToken);

            // If we end up here, it means that internally we decided to terminate the service because
            // we no longer have any clients
            Environment.Exit(0);
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception)
        {
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

    private readonly List<uint> _registrationCookies = new();

    public void RegisterClass<T>(Guid clsid)
        where T : new()
    {
        uint cookie;

        HRESULT hr = PInvoke.CoRegisterClassObject(clsid, new BasicClassWinRTFactory<T>(), CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE | REGCLS.REGCLS_SUSPENDED, out cookie);
        Marshal.ThrowExceptionForHR(hr);

        _registrationCookies.Add(cookie);

        hr = PInvoke.CoResumeClassObjects();
        Marshal.ThrowExceptionForHR(hr);
    }
}
