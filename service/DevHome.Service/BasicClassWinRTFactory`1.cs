// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.Service;
using Windows.Win32.Foundation;
using WinRT;

namespace COMRegistration;

[ComVisible(true)]
public class BasicClassWinRTFactory<T> : IClassFactory
where T : new()
{
    public BasicClassWinRTFactory()
    {
    }

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(HRESULT.CLASS_E_NOAGGREGATION);
        }

        if (riid == typeof(T).GUID || riid == Guid.Parse(Guids.IUnknown))
        {
            try
            {
                // Create the instance of the WinRT object
                ppvObject = MarshalInspectable<T>.FromManaged(new T());
            }
            catch (Exception)
            {
                // We failed creating an object (possibly due to access denied). If we were just spun up
                // to handle this (failed) activation, shut down our service.
                if (ServiceLifetimeController.CanUnload())
                {
                    WindowsBackgroundService.Stop();
                }

                throw;
            }
        }
        else
        {
            // The object that ppvObject points to does not support the
            // interface identified by riid.
            Marshal.ThrowExceptionForHR(HRESULT.E_NOINTERFACE);
        }

        return 0;
    }

    int IClassFactory.LockServer(bool fLock)
    {
        return 0;
    }
}

internal static class Guids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
}

// IClassFactory declaration
[ComImport]
[ComVisible(false)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid(Guids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}
