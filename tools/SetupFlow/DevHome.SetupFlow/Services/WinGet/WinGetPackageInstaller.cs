// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Extensions;
using DevHome.SetupFlow.Models;
using Microsoft.Management.Deployment;
using Windows.Win32.Foundation;
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Services.WinGet;
public class WinGetPackageInstaller : IWinGetPackageInstaller
{
    private readonly WindowsPackageManagerFactory _wingetFactory;
    private readonly IWinGetPackageFinder _packageFinder;

    public WinGetPackageInstaller(WindowsPackageManagerFactory wingetFactory, IWinGetPackageFinder packageFinder)
    {
        _wingetFactory = wingetFactory;
        _packageFinder = packageFinder;
    }

    public async Task<InstallPackageResult> InstallPackageAsync(WPMPackageCatalog catalog, string packageId)
    {
        // 1. Find package
        var findOptions = _wingetFactory.CreateFindPackagesOptions();
        var filter = _wingetFactory.CreatePackageMatchFilter(PackageMatchField.Id, PackageFieldMatchOption.Equals, packageId);
        findOptions.Filters.Add(filter);
        var findResult = await catalog.FindPackagesAsync(findOptions);
        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Install aborted for package {packageId} because the find operation failed with status ");
            throw new FindPackagesException(findResult.Status);
        }

        if (findResult.Matches.Count == 0)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Install aborted for package {packageId} because it was not found in the provided catalog");
            throw new FindPackagesException(FindPackagesResultStatus.CatalogError);
        }

        // 2. Install package
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting package install for {packageId}");
        var installOptions = _wingetFactory.CreateInstallOptions();
        installOptions.PackageInstallMode = PackageInstallMode.Silent;
        var packageManager = _wingetFactory.CreatePackageManager();
        var installResult = await packageManager.InstallPackageAsync(findResult.Matches[0].CatalogPackage, installOptions).AsTask();
        var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;
        var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK); // WPM API V4

        // 3. Report install result
        Log.Logger?.ReportInfo(
            Log.Component.AppManagement,
            $"Install result: Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");
        if (installResult.Status != InstallResultStatus.Ok)
        {
            throw new InstallPackageException(installResult.Status, extendedErrorCode, installErrorCode);
        }

        return new InstallPackageResult()
        {
            ExtendedErrorCode = extendedErrorCode,
            RebootRequired = installResult.RebootRequired,
        };
    }
}
