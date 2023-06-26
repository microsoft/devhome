// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;
using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Microsoft.Management.Deployment;
using Windows.Foundation;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent.Tasks;

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
//// https://github.com/microsoft/devhome/issues/622
public sealed class ElevatedInstallTask
{
    private readonly WindowsPackageManagerFactory _wingetFactory = new WindowsPackageManagerManualActivationFactory();

    /// <summary>
    /// Installs a package given its ID and the ID of the catalog it comes from.
    /// </summary>
    public IAsyncOperation<ElevatedInstallTaskResult> InstallPackage(string packageId, string catalogName)
    {
        return Task.Run(async () =>
        {
            var result = new ElevatedInstallTaskResult();
            try
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Elevated install requested for package [{packageId}] from catalog [{catalogName}]");

                var packageManager = _wingetFactory.CreatePackageManager();

                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Connecting to catalog [{catalogName}]");
                var catalogReference = packageManager.GetPackageCatalogByName(catalogName);
                var connectResult = await catalogReference.ConnectAsync();
                if (connectResult.Status != ConnectResultStatus.Ok)
                {
                    Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to the catalog [{catalogName}] with status {connectResult.Status}");
                    result.TaskAttempted = false;
                    return result;
                }

                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Finding package [{packageId}] in catalog");
                var findOptions = CreateFindOptionsForPackageId(packageId);
                var findResult = connectResult.PackageCatalog.FindPackages(findOptions);
                if (findResult.Status != FindPackagesResultStatus.Ok
                    || findResult.Matches.Count < 1
                    || findResult.WasLimitExceeded)
                {
                    Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to find package. Status={findResult.Status}, Matches Count={findResult.Matches.Count}, LimitReached={findResult.WasLimitExceeded}");
                    result.TaskAttempted = false;
                    return result;
                }

                var packageToInstall = findResult.Matches[0].CatalogPackage;

                var installOptions = _wingetFactory.CreateInstallOptions();
                installOptions.PackageInstallMode = PackageInstallMode.Silent;

                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Initiating install of package {packageId}");
                var installResult = await packageManager.InstallPackageAsync(packageToInstall, installOptions);
                var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;

                // Contract version 4
                var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK);

                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Install finished. Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");
                result.TaskAttempted = true;
                result.TaskSucceeded = installResult.Status == InstallResultStatus.Ok;
                result.RebootRequired = installResult.RebootRequired;
                result.Status = (int)installResult.Status;
                result.ExtendedErrorCode = extendedErrorCode;
                result.InstallerErrorCode = installErrorCode;

                return result;
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, "Elevated app install failed.", e);
                result.TaskSucceeded = false;
            }

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
