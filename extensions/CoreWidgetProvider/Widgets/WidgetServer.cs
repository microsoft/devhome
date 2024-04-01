// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using COM;
using Microsoft.Windows.Widgets.Providers;
using Serilog;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace CoreWidgetProvider.Widgets;

public sealed class WidgetServer : IDisposable
{
    private readonly HashSet<uint> registrationCookies = new();

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2050:COMCorrectness",
        Justification = "WidgetProviderFactory and all the interfaces it implements are defined in an assembly that is not marked trimmable which means the relevant interfaces won't be trimmed.")]
    public void RegisterWidget<T>(Func<T> createWidget)
        where T : IWidgetProvider
    {
        Log.Debug($"Registering class object:");
        Log.Debug($"CLSID: {typeof(T).GUID:B}");
        Log.Debug($"Type: {typeof(T)}");

        uint cookie;
        var clsid = typeof(T).GUID;
        var hr = PInvoke.CoRegisterClassObject(
            clsid,
            new WidgetProviderFactory<T>(createWidget),
            CLSCTX.CLSCTX_LOCAL_SERVER,
            Ole32.REGCLS_MULTIPLEUSE | Ole32.REGCLS_SUSPENDED,
            out cookie);

        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        registrationCookies.Add(cookie);
        Log.Debug($"Cookie: {cookie}");
        hr = PInvoke.CoResumeClassObjects();
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    public void Run()
    {
        // TODO : We need to handle lifetime management of the server.
        // For details around ref counting and locking of out-of-proc COM servers, see
        // https://docs.microsoft.com/windows/win32/com/out-of-process-server-implementation-helpers
        // https://github.com/microsoft/devhome/issues/645
        Console.ReadLine();
    }

    public void Dispose()
    {
        Log.Debug($"Revoking class object registrations:");
        foreach (var cookie in registrationCookies)
        {
            Log.Debug($"Cookie: {cookie}");
            var hr = PInvoke.CoRevokeClassObject(cookie);
            Debug.Assert(hr >= 0, $"CoRevokeClassObject failed ({hr:x}). Cookie: {cookie}");
        }
    }

    private sealed class Ole32
    {
#pragma warning disable SA1310 // Field names should not contain underscore
        // https://docs.microsoft.com/windows/win32/api/combaseapi/ne-combaseapi-regcls
        public const REGCLS REGCLS_MULTIPLEUSE = (REGCLS)1;
        public const REGCLS REGCLS_SUSPENDED = (REGCLS)4;
#pragma warning restore SA1310 // Field names should not contain underscore
    }
}
