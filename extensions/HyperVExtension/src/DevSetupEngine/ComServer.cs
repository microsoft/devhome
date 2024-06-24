// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace HyperVExtension.DevSetupEngine;

/// <summary>
/// Helper to register COM class factories at runtime.
/// </summary>
internal sealed class ComServer : IDisposable
{
    private readonly HashSet<uint> registrationCookies = new();

    public void RegisterComServer<T>(Func<T> createExtension)
    {
        Trace.WriteLine($"Registering class object:");
        Trace.Indent();
        Trace.WriteLine($"CLSID: {typeof(T).GUID:B}");
        Trace.WriteLine($"Type: {typeof(T)}");

        uint cookie;
        var clsid = typeof(T).GUID;
        var hr = PInvoke.CoRegisterClassObject(
            in clsid,
            new DevSetupEngineClassFactory<T>(createExtension),
            CLSCTX.CLSCTX_LOCAL_SERVER,
            REGCLS.REGCLS_MULTIPLEUSE | REGCLS.REGCLS_SUSPENDED,
            out cookie);

        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        registrationCookies.Add(cookie);
        Trace.WriteLine($"Cookie: {cookie}");
        Trace.Unindent();

        hr = PInvoke.CoResumeClassObjects();
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    public void Dispose()
    {
        Trace.WriteLine($"Revoking class object registrations:");
        Trace.Indent();
        foreach (var cookie in registrationCookies)
        {
            Trace.WriteLine($"Cookie: {cookie}");
            var hr = PInvoke.CoRevokeClassObject(cookie);
            Debug.Assert(hr >= 0, $"CoRevokeClassObject failed ({hr:x}). Cookie: {cookie}");
        }

        Trace.Unindent();
    }
}
