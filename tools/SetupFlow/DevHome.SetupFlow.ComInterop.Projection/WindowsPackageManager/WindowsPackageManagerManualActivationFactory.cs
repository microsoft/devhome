// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using WinRT;

namespace DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;

/// <summary>
/// Factory for creating winget COM objects using manual activation
/// to have them in an elevated context.
/// </summary>
/// <remarks>
/// This needs to be called from an elevated context, or the winget
/// server will reject the connection.
///
/// The WinGetServerManualActivation_CreateInstance function used here is defined in
/// https://github.com/microsoft/winget-cli/blob/master/src/WinGetServer/WinGetServerManualActivation_Client.cpp
///
/// This class is based on what the winget cmdlets do. See
/// https://github.com/microsoft/winget-cli/blob/master/src/PowerShell/Microsoft.WinGet.Client/Helpers/ComObjectFactory.cs
/// </remarks>
public class WindowsPackageManagerManualActivationFactory : WindowsPackageManagerFactory
{
    // The only CLSID context supported by the DLL we call is Prod.
    // If we want to use Dev classes we have to use a Dev version of the DLL.
    public WindowsPackageManagerManualActivationFactory()
        : base(ClsidContext.Prod)
    {
    }

    protected override T CreateInstance<T>(Guid clsid, Guid iid)
    {
        var hr = WinGetServerManualActivation_CreateInstance(clsid, iid, 0, out var instance);
        Marshal.ThrowExceptionForHR(hr);

        IntPtr pointer = Marshal.GetIUnknownForObject(instance);
        return MarshalInterface<T>.FromAbi(pointer);
    }

    [DllImport("winrtact.dll", EntryPoint = "WinGetServerManualActivation_CreateInstance", ExactSpelling = true, PreserveSig = true)]
    private static extern int WinGetServerManualActivation_CreateInstance(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid iid,
        uint flags,
        [Out, MarshalAs(UnmanagedType.IUnknown)] out object instance);
}
