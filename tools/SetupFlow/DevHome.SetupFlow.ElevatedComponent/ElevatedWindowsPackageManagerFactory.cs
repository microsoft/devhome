// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using Microsoft.Management.Deployment;
using WinRT;

namespace DevHome.SetupFlow.ElevatedComponent;

internal class ElevatedWindowsPackageManagerFactory : IWindowsPackageManagerFactory
{
    private static readonly Guid PackageManagerClsid = Guid.Parse("C53A4F16-787E-42A4-B304-29EFFB4BF597");
    private static readonly Guid FindPackagesOptionsClsid = Guid.Parse("572DED96-9C60-4526-8F92-EE7D91D38C1A");
    private static readonly Guid CreateCompositePackageCatalogOptionsClsid = Guid.Parse("526534B8-7E46-47C8-8416-B1685C327D37");
    private static readonly Guid InstallOptionsClsid = Guid.Parse("1095F097-EB96-453B-B4E6-1613637F3B14");
    private static readonly Guid UninstallOptionsClsid = Guid.Parse("E1D9A11E-9F85-4D87-9C17-2B93143ADB8D");
    private static readonly Guid PackageMatchFilterClsid = Guid.Parse("D02C9DAF-99DC-429C-B503-4E504E4AB000");

    private static readonly Type PackageManagerType = Type.GetTypeFromCLSID(PackageManagerClsid)!;
    private static readonly Type FindPackagesOptionsType = Type.GetTypeFromCLSID(FindPackagesOptionsClsid)!;
    private static readonly Type CreateCompositePackageCatalogOptionsType = Type.GetTypeFromCLSID(CreateCompositePackageCatalogOptionsClsid)!;
    private static readonly Type InstallOptionsType = Type.GetTypeFromCLSID(InstallOptionsClsid)!;
    private static readonly Type UninstallOptionsType = Type.GetTypeFromCLSID(UninstallOptionsClsid)!;
    private static readonly Type PackageMatchFilterType = Type.GetTypeFromCLSID(PackageMatchFilterClsid)!;

    private static readonly Guid PackageManagerIid = Guid.Parse("B375E3B9-F2E0-5C93-87A7-B67497F7E593");
    private static readonly Guid FindPackagesOptionsIid = Guid.Parse("A5270EDD-7DA7-57A3-BACE-F2593553561F");
    private static readonly Guid CreateCompositePackageCatalogOptionsIid = Guid.Parse("21ABAA76-089D-51C5-A745-C85EEFE70116");
    private static readonly Guid InstallOptionsIid = Guid.Parse("6EE9DB69-AB48-5E72-A474-33A924CD23B3");
    private static readonly Guid UninstallOptionsIid = Guid.Parse("3EBC67F0-8339-594B-8A42-F90B69D02BBE");
    private static readonly Guid PackageMatchFilterIid = Guid.Parse("D981ECA3-4DE5-5AD7-967A-698C7D60FC3B");

    /// <summary>
    /// Creates an instance of the <see cref="PackageManager" /> class.
    /// </summary>
    /// <returns>A <see cref="PackageManager" /> instance.</returns>
    public PackageManager CreatePackageManager()
    {
        return Create<PackageManager>(PackageManagerType, PackageManagerIid);
    }

    /// <summary>
    /// Creates an instance of the <see cref="FindPackagesOptions" /> class.
    /// </summary>
    /// <returns>A <see cref="FindPackagesOptions" /> instance.</returns>
    public FindPackagesOptions CreateFindPackagesOptions()
    {
        return Create<FindPackagesOptions>(FindPackagesOptionsType, FindPackagesOptionsIid);
    }

    /// <summary>
    /// Creates an instance of the <see cref="CreateCompositePackageCatalogOptions" /> class.
    /// </summary>
    /// <returns>A <see cref="CreateCompositePackageCatalogOptions" /> instance.</returns>
    public CreateCompositePackageCatalogOptions CreateCreateCompositePackageCatalogOptions()
    {
        return Create<CreateCompositePackageCatalogOptions>(CreateCompositePackageCatalogOptionsType, CreateCompositePackageCatalogOptionsIid);
    }

    /// <summary>
    /// Creates an instance of the <see cref="InstallOptions" /> class.
    /// </summary>
    /// <returns>An <see cref="InstallOptions" /> instance.</returns>
    public InstallOptions CreateInstallOptions()
    {
        return Create<InstallOptions>(InstallOptionsType, InstallOptionsIid);
    }

    /// <summary>
    /// Creates an instance of the <see cref="UninstallOptions" /> class.
    /// </summary>
    /// <returns>A <see cref="UninstallOptions" /> instance.</returns>
    public UninstallOptions CreateUninstallOptions()
    {
        return Create<UninstallOptions>(UninstallOptionsType, UninstallOptionsIid);
    }

    /// <summary>
    /// Creates an instance of the <see cref="PackageMatchFilter" /> class.
    /// </summary>
    /// <returns>A <see cref="PackageMatchFilter" /> instance.</returns>
    public PackageMatchFilter CreatePackageMatchFilter()
    {
        return Create<PackageMatchFilter>(PackageMatchFilterType, PackageMatchFilterIid);
    }

    private static T Create<T>(Type type, in Guid iid)
    {
        Console.WriteLine($"Creating {type.Name}; GUID = {type.GUID}; IID = {iid}");

        var hr = WinGetServerManualActivation_CreateInstance(type.GUID, iid, 0, out var instance);
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
