// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.Services.WindowsPackageManager.Models;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Windows.Foundation;

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
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ElevatedInstallTask));

    /// <summary>
    /// Installs a package given its ID and the ID of the catalog it comes from.
    /// </summary>
    public IAsyncOperation<ElevatedInstallTaskResult> InstallPackage(string packageId, string catalogName, string version)
    {
        return Task.Run(async () =>
        {
            var result = new ElevatedInstallTaskResult();
            try
            {
                var winget = ElevatedComponentOperation.Host.Services.GetRequiredService<IWinGet>();
                var installResult = await winget.InstallPackageAsync(new WinGetPackageUri(catalogName, packageId, new(version)));
                result.TaskAttempted = true;
                result.RebootRequired = installResult.RebootRequired;

                // Set the extended error code in case a reboot is required
                result.ExtendedErrorCode = installResult.ExtendedErrorCode;
                result.TaskSucceeded = true;
            }
            catch (InstallPackageException e)
            {
                result.Status = (int)e.Status;
                result.ExtendedErrorCode = e.ExtendedErrorCode;
                result.InstallerErrorCode = e.InstallerErrorCode;
                result.TaskSucceeded = false;
                _log.Error($"Failed to install package with status {e.Status} and installer error code {e.InstallerErrorCode}");
            }
            catch (Exception e)
            {
                result.TaskSucceeded = false;
                _log.Error(e, $"Exception thrown while installing package.");
            }

            return result;
        }).AsAsyncOperation();
    }
}
