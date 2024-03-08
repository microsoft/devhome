// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using WinRT;

namespace HyperVExtension.DevSetupEngine;

/// <summary>
/// Helper COM class factory that creates a new instance of T.
/// </summary>
/// <typeparam name="T">Class with a GUID matching COM class GUID.</typeparam>
[ComVisible(true)]
#pragma warning disable SA1649 // File name should match first type name
internal sealed class DevSetupEngineClassFactory<T> : IClassFactory
#pragma warning restore SA1649 // File name should match first type name
{
#pragma warning disable SA1310 // Field names should not contain underscore

    private static readonly Guid IID_IUnknown = Guid.Parse("00000000-0000-0000-C000-000000000046");

#pragma warning restore SA1310 // Field names should not contain underscore

    private readonly Func<T> _createClassInstance;

    public DevSetupEngineClassFactory(Func<T> createClassInstance)
    {
        _createClassInstance = createClassInstance;
    }

    public void CreateInstance(
        [MarshalAs(UnmanagedType.Interface)] object outerIUnknown,
        ref Guid interfaceId,
        out IntPtr resultObject)
    {
        resultObject = IntPtr.Zero;

        if (outerIUnknown != null)
        {
            Marshal.ThrowExceptionForHR(HRESULT.CLASS_E_NOAGGREGATION);
        }

        if (interfaceId == typeof(T).GUID || interfaceId == IID_IUnknown)
        {
            // Create the instance of the .NET object
            resultObject = MarshalInspectable<object>.FromManaged(_createClassInstance()!);
        }
        else
        {
            Marshal.ThrowExceptionForHR(HRESULT.E_NOINTERFACE);
        }
    }

    public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
    {
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
