// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
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
            Log.Logger?.ReportInfo(nameof(PackageInstaller), $"Elevated install requested for package [{packageId}] from catalog [{catalogName}]");
            var result = new ElevatedInstallResult();

            var packageManager = _wingetFactory.CreatePackageManager();

            Log.Logger?.ReportInfo(nameof(PackageInstaller), $"Connecting to catalog [{catalogName}]");
            var catalogReference = packageManager.GetPackageCatalogByName(catalogName);
            var connectResult = await catalogReference.ConnectAsync();
            if (connectResult.Status != ConnectResultStatus.Ok)
            {
                Log.Logger?.ReportError(nameof(PackageInstaller), $"Failed to connect to the catalog [{catalogName}] with status {connectResult.Status}");
                result.InstallAttempted = false;
                return result;
            }

            Log.Logger?.ReportInfo(nameof(PackageInstaller), $"Finding package [{packageId}] in catalog");
            var findOptions = CreateFindOptionsForPackageId(packageId);
            var findResult = connectResult.PackageCatalog.FindPackages(findOptions);
            if (findResult.Status != FindPackagesResultStatus.Ok
                || findResult.Matches.Count < 1
                || findResult.WasLimitExceeded)
            {
                Log.Logger?.ReportError(nameof(PackageInstaller), $"Failed to find package. Status={findResult.Status}, Matches Count={findResult.Matches.Count}, LimitReached={findResult.WasLimitExceeded}");
                result.InstallAttempted = false;
                return result;
            }

            var packageToInstall = findResult.Matches[0].CatalogPackage;

            var installOptions = _wingetFactory.CreateInstallOptions();
            installOptions.PackageInstallMode = PackageInstallMode.Silent;

            Log.Logger?.ReportInfo(nameof(PackageInstaller), $"Initiating install of package {packageId}");
            var installResult = await packageManager.InstallPackageAsync(packageToInstall, installOptions);
            Log.Logger?.ReportInfo(nameof(PackageInstaller), $"Install finished. Status={installResult.Status}, InstallerErrorCode={installResult.InstallerErrorCode}, RebootRequired={installResult.RebootRequired}");
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
