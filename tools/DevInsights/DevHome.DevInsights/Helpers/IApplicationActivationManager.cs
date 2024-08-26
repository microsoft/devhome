// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace DevHome.DevInsights.Helpers;

[ComImport]
[Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IApplicationActivationManager
{
    int ActivateApplication(
        [In] string appUserModelId,
        [In] string arguments,
        [In] ACTIVATEOPTIONS options,
        [Out] out uint processId);
}

public enum ACTIVATEOPTIONS
{
    None = 0x00000000,
    DesignMode = 0x00000001,
    NoErrorUI = 0x00000002,
    NoSplashScreen = 0x00000004,
}

public static class CLSID
{
    public static readonly Guid ApplicationActivationManager = new("45BA127D-10A8-46EA-8AB7-56EA9078943C");
}

public static class HResult
{
    public static bool Succeeded(int hr) => hr >= 0;

    public static bool Failed(int hr) => hr < 0;
}
