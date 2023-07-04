// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using Microsoft.Management.Deployment;
using Windows.Foundation;

namespace DevHome.SetupFlow.TaskOperator;
public class InstallOperator : IInstallPackageOperator
{
    private readonly IWindowsPackageManager _wpm;

    public InstallOperator(IWindowsPackageManager wpm)
    {
        _wpm = wpm;
    }

    public async Task<IInstallPackageResult> InstallPackageAsync(WinGetPackage package)
    {
        try
        {
            var installResult = await _wpm.InstallPackageAsync(package);
            return new InstallPackageResult
            {
                Succeeded = true,
                RebootRequired = installResult.RebootRequired,
                ExtendedErrorCode = installResult.ExtendedErrorCode,
            };
        }
        catch (InstallPackageException e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to install package {package.Id} from catalog {package.CatalogName}", e);
            return new InstallPackageResult
            {
                Status = (int)e.Status,
                ExtendedErrorCode = e.ExtendedErrorCode,
                InstallerErrorCode = e.InstallerErrorCode,
                Succeeded = false,
            };
        }
    }

    public IAsyncOperation<IInstallPackageResult> InstallPackageAsync(string packageId, string catalogName)
    {
        return Task.Run<IInstallPackageResult>(async () =>
        {
            try
            {
                // Search only remote catalog
                var catalog = _wpm.CreateCatalogByName(CompositeSearchBehavior.RemotePackagesFromRemoteCatalogs, catalogName);
                await catalog.ConnectAsync(forceReconnect: false);
                var matches = await catalog.GetPackagesAsync(new HashSet<string>() { packageId });
                if (!matches.Any())
                {
                    Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to find package {packageId}");
                    return new InstallPackageResult
                    {
                        Succeeded = false,
                    };
                }

                return await InstallPackageAsync((WinGetPackage)matches[0]);
            }
            catch (CatalogConnectionException e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to connect to the catalog [{catalogName}]", e);
                return new InstallPackageResult
                {
                    Succeeded = false,
                };
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Unexpected error occurred when attempting to install package {packageId} from catalog {catalogName}", e);
                return new InstallPackageResult
                {
                    Succeeded = false,
                };
            }
        }).AsAsyncOperation();
    }
}
