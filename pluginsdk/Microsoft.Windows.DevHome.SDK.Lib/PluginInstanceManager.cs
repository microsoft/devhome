// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Rpc;
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

    private const RPC_STATUS RPC_S_NO_CALL_ACTIVE = (RPC_STATUS)1725;

    private const RPC_STATUS RPC_S_OK = (RPC_STATUS)0;

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
        bool succeeded = false;
        uint callerProcessId = 0;
        (succeeded, callerProcessId) = GetCallerProcessId();
        if (succeeded)
        {
            // Get the caller process id and use it to check if the caller has permissions to access the feature.
            AppCapabilityAccessStatus status = AppCapabilityAccessStatus.DeniedBySystem;

            AppCapability capability = AppCapability.CreateWithProcessIdForUser(null, "DevHomePluginHost", callerProcessId);
            status = capability.CheckAccess();

            return (status == AppCapabilityAccessStatus.Allowed);
        }

        return false;
    }

    private static ulong HandleToULong(HANDLE h)
    {
        return (ulong)(IntPtr)h;
    }

    unsafe private (bool, uint) GetCallerProcessId()
    {
        RPC_STATUS rpcStatus = 0;
        RPC_CALL_ATTRIBUTES_V2_W callAttributes;
        callAttributes.Version = 2;
        callAttributes.Flags = 0x10;

        rpcStatus = PInvoke.RpcServerInqCallAttributes(null, (void*)&callAttributes);
        
        if (rpcStatus == RPC_S_NO_CALL_ACTIVE ||
            (rpcStatus == RPC_S_OK && HandleToULong(callAttributes.ClientPID) == PInvoke.GetCurrentProcessId()))
        {
            return (true, PInvoke.GetCurrentProcessId());
        }
        else if (rpcStatus == RPC_S_OK)
        {
            // out-of-proc case.
            return (true, (uint)HandleToULong(callAttributes.ClientPID));
        }
        else
        {
            return (false, 0);
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
