// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;

public class WindowsPackageManagerDefaultFactory : WindowsPackageManagerFactory
{
    public WindowsPackageManagerDefaultFactory(ClsidContext clsidContext = ClsidContext.Prod)
        : base(clsidContext)
    {
    }

    protected override T CreateInstance<T>(Guid clsid, Guid iid)
    {
        PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, iid, out var result);
        return MarshalGeneric<T>.FromAbi(Marshal.GetIUnknownForObject(result));
    }
}
