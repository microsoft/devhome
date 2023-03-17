// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Storage;
using WinRT;

namespace Microsoft.Windows.DevHome.SDK;

[ComVisible(true)]
internal class PluginInstanceManager<T> : IClassFactory
    where T : IPlugin
{
#pragma warning disable SA1310 // Field names should not contain underscore

    private const int E_NOINTERFACE = unchecked((int)0x80004002);

    private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);

    private const int E_ACCESSDENIED = unchecked((int)0x80070005);

    private static readonly Guid IID_IUnknown = Guid.Parse("00000000-0000-0000-C000-000000000046");

#pragma warning restore SA1310 // Field names should not contain underscore

    private readonly Func<T> _createPlugin;

    private readonly bool _restrictCallers;

    public PluginInstanceManager(Func<T> createPlugin, bool restrictCallers)
    {
        this._createPlugin = createPlugin;
        this._restrictCallers = restrictCallers;
    }

    public void CreateInstance(
        [MarshalAs(UnmanagedType.Interface)] object pUnkOuter,
        ref Guid riid,
        out IntPtr ppvObject)
    {
        if (_restrictCallers && !ComCallerHasPluginHostCapability())
        {
            Marshal.ThrowExceptionForHR(E_ACCESSDENIED);
        }

        ppvObject = IntPtr.Zero;

        if (pUnkOuter != null)
        {
            Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
        }

        if (riid == typeof(T).GUID || riid == IID_IUnknown)
        {
            // Create the instance of the .NET object
            ppvObject = MarshalInspectable<object>.FromManaged(_createPlugin());
        }
        else
        {
            // The object that ppvObject points to does not support the
            // interface identified by riid.
            Marshal.ThrowExceptionForHR(E_NOINTERFACE);
        }
    }

    public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
    {
    }

    private bool ComCallerHasPluginHostCapability()
    {

        return false;
    }
}

// https://docs.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iclassfactory
[ComImport]
[ComVisible(false)]
[Guid("00000001-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactory
{
    void CreateInstance(
        [MarshalAs(UnmanagedType.Interface)] object pUnkOuter,
        ref Guid riid,
        out IntPtr ppvObject);

    void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}
