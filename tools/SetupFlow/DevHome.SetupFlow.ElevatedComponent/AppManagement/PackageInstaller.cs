// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using Microsoft.Management.Deployment;
using Windows.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent.AppManagement;

/// <summary>
/// Class for installing winget packages.
/// </summary>
/// <remarks>
/// This is intended to install packages that require admin permissions.
/// Running in an elevated context should prevent us from getting a
/// UAC prompt when starting the installer.
///
/// We cannot use the objects we winget COM objects we already created
/// during the Setup flow here because those live in a different
/// non-elevated process. Since we cannot pass complicated objects
/// unless they can be projected by CsWinRT, we go the easy route
/// and install given the package and catalog IDs.
/// </remarks>
//// TODO: Some of this can be refactored to avoid duplication with non-elevated installs
public sealed class PackageInstaller
{
    private readonly WindowsPackageManagerFactory _wingetFactory = new WindowsPackageManagerManualActivationFactory();

    /// <summary>
    /// Installs a package given its ID and the ID of the catalog it comes from.
    /// </summary>
    public IAsyncOperation<ElevatedInstallResult> InstallPackage(string packageId, string catalogName)
    {
        return Task.Run(async () =>
        {
            var result = new ElevatedInstallResult();

            var packageManager = _wingetFactory.CreatePackageManager();

            var catalogReference = packageManager.GetPackageCatalogByName(catalogName);
            var connectResult = await catalogReference.ConnectAsync();
            if (connectResult.Status != ConnectResultStatus.Ok)
            {
                result.InstallAttempted = false;
                return result;
            }

            var findOptions = CreateFindOptionsForPackageId(packageId);
            var findResult = connectResult.PackageCatalog.FindPackages(findOptions);
            if (findResult.Status != FindPackagesResultStatus.Ok
                || findResult.Matches.Count < 1
                || findResult.WasLimitExceeded)
            {
                result.InstallAttempted = false;
                return result;
            }

            var packageToInstall = findResult.Matches[0].CatalogPackage;

            var installOptions = _wingetFactory.CreateInstallOptions();
            installOptions.PackageInstallMode = PackageInstallMode.Silent;

            var installResult = await packageManager.InstallPackageAsync(packageToInstall, installOptions);
            result.InstallAttempted = true;
            result.InstallSucceeded = installResult.Status == InstallResultStatus.Ok;
            result.RebootRequired = installResult.RebootRequired;

            return result;
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Creates a <see cref="FindPackagesOptions"/> that can be used to find
    /// the package with the given ID.
    /// </summary>
    private FindPackagesOptions CreateFindOptionsForPackageId(string packageId)
    {
        var matchFilter = _wingetFactory.CreatePackageMatchFilter();
        matchFilter.Field = PackageMatchField.Id;
        matchFilter.Option = PackageFieldMatchOption.Equals;
        matchFilter.Value = packageId;

        var findOptions = _wingetFactory.CreateFindPackagesOptions();
        findOptions.Selectors.Add(matchFilter);
        findOptions.ResultLimit = 1;

        return findOptions;
    }
}
