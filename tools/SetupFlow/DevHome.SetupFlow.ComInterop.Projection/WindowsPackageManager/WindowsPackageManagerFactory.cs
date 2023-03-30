// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Management.Deployment;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;

/// <summary>
/// Factory class for creating WinGet COM objects.
/// Details about each method can be found in the source IDL:
/// https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </summary>
public class WindowsPackageManagerFactory : IWindowsPackageManagerFactory
{
    private readonly ClsidContext _clsidContext;

    public WindowsPackageManagerFactory(ClsidContext clsidContext = ClsidContext.Prod)
    {
        _clsidContext = clsidContext;
    }

    public PackageManager CreatePackageManager() => CreateInstance<PackageManager>();

    public FindPackagesOptions CreateFindPackagesOptions() => CreateInstance<FindPackagesOptions>();

    public CreateCompositePackageCatalogOptions CreateCreateCompositePackageCatalogOptions() => CreateInstance<CreateCompositePackageCatalogOptions>();

    public InstallOptions CreateInstallOptions() => CreateInstance<InstallOptions>();

    public UninstallOptions CreateUninstallOptions() => CreateInstance<UninstallOptions>();

    public PackageMatchFilter CreatePackageMatchFilter() => CreateInstance<PackageMatchFilter>();

    private TClass CreateInstance<TClass>()
    {
        var clsid = ClassesDefinition.GetClsid<TClass>(_clsidContext);
        var iid = ClassesDefinition.GetIid<TClass>();
        PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, iid, out var result);
        return MarshalGeneric<TClass>.FromAbi(Marshal.GetIUnknownForObject(result));
    }
}
