// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Contract.TaskOperator;
using Microsoft.Management.Deployment;
using Windows.Foundation;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.TaskOperator;
public class InstallOperator : IInstallOperator
{
    private readonly WindowsPackageManagerFactory _wingetFactory = new WindowsPackageManagerManualActivationFactory();

    public IAsyncOperation<IInstallPackageResult> InstallPackageAsync(string packageId, string catalogName)
    {
        return Task.Run<IInstallPackageResult>(async () =>
        {
            var result = new InstallPackageResult();
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
                    result.Attempted = false;
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
                    result.Attempted = false;
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
                result.Attempted = true;
                result.Succeeded = installResult.Status == InstallResultStatus.Ok;
                result.RebootRequired = installResult.RebootRequired;
                result.Status = (int)installResult.Status;
                result.ExtendedErrorCode = extendedErrorCode;
                result.InstallerErrorCode = installErrorCode;

                return result;
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, "Elevated app install failed.", e);
                result.Succeeded = false;
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

public class InstallPackageResult : IInstallPackageResult
{
    public int ExtendedErrorCode { get; set; }

    public uint InstallerErrorCode { get; set; }

    public int Status { get; set; }

    public bool Attempted { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}
