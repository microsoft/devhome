// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
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

    private static HANDLE CURRENT_THREAD_PSEUDO_HANDLE = (HANDLE)(IntPtr)(-6);

    private static readonly Guid IID_IUnknown = Guid.Parse("00000000-0000-0000-C000-000000000046");

#pragma warning restore SA1310 // Field names should not contain underscore

    private readonly Func<T> _createPlugin;

    private readonly bool _restrictToMicrosoftPluginHosts;

    public PluginInstanceManager(Func<T> createPlugin, bool restrictToMicrosoftPluginHosts)
    {
        this._createPlugin = createPlugin;
        this._restrictToMicrosoftPluginHosts = restrictToMicrosoftPluginHosts;
    }

    public void CreateInstance(
        [MarshalAs(UnmanagedType.Interface)] object pUnkOuter,
        ref Guid riid,
        out IntPtr ppvObject)
    {
        if (_restrictToMicrosoftPluginHosts && !IsMicrosoftPluginHost())
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

    private unsafe bool IsMicrosoftPluginHost()
    {
        if (PInvoke.CoImpersonateClient() != 0)
        {
            return false;
        }

        uint buffer = 0;
        if (PInvoke.GetPackageFamilyNameFromToken(CURRENT_THREAD_PSEUDO_HANDLE, &buffer, null) != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
        {
            return false;
        }

        var value = new char[buffer];
        fixed (char* p = value)
        {
            if (PInvoke.GetPackageFamilyNameFromToken(CURRENT_THREAD_PSEUDO_HANDLE, &buffer, p) != 0)
            {
                return false;
            }
        }

        if (PInvoke.CoRevertToSelf() != 0)
        {
            return false;
        }

        var valueStr = new string(value);
        switch (valueStr)
        {
            case "Microsoft.Windows.DevHome_8wekyb3d8bbwe\0":
            case "Microsoft.WindowsTerminal\0":
            case "Microsoft.WindowsTerminal_8wekyb3d8bbwe\0":
            case "WindowsTerminalDev_8wekyb3d8bbwe\0":
            case "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe\0":
                return true;
            default:
                return false;
        }
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
