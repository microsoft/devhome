// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;

public interface IWindowsPackageManagerFactory
{
    CreateCompositePackageCatalogOptions CreateCreateCompositePackageCatalogOptions();

    FindPackagesOptions CreateFindPackagesOptions();

    InstallOptions CreateInstallOptions();

    PackageManager CreatePackageManager();

    PackageMatchFilter CreatePackageMatchFilter();

    UninstallOptions CreateUninstallOptions();
}
