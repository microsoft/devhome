// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.Common.Extensions;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Microsoft.Management.Deployment;
using Serilog;
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
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ElevatedInstallTask));
    private readonly WindowsPackageManagerFactory _wingetFactory = new WindowsPackageManagerManualActivationFactory();

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
                _log.Information($"Elevated install requested for package [{packageId}] from catalog [{catalogName}]");

                var packageManager = _wingetFactory.CreatePackageManager();

                _log.Information($"Connecting to catalog [{catalogName}]");
                var catalogReference = packageManager.GetPackageCatalogByName(catalogName);
                var connectResult = await catalogReference.ConnectAsync();
                if (connectResult.Status != ConnectResultStatus.Ok)
                {
                    _log.Error($"Failed to connect to the catalog [{catalogName}] with status {connectResult.Status}");
                    result.TaskAttempted = false;
                    return result;
                }

                _log.Information($"Finding package [{packageId}] in catalog");
                var findOptions = CreateFindOptionsForPackageId(packageId);
                var findResult = connectResult.PackageCatalog.FindPackages(findOptions);
                if (findResult.Status != FindPackagesResultStatus.Ok
                    || findResult.Matches.Count < 1
                    || findResult.WasLimitExceeded)
                {
                    _log.Error($"Failed to find package. Status={findResult.Status}, Matches Count={findResult.Matches.Count}, LimitReached={findResult.WasLimitExceeded}");
                    result.TaskAttempted = false;
                    return result;
                }

                var packageToInstall = findResult.Matches[0].CatalogPackage;

                var installOptions = _wingetFactory.CreateInstallOptions();
                installOptions.PackageInstallMode = PackageInstallMode.Silent;
                if (!string.IsNullOrWhiteSpace(version))
                {
                    installOptions.PackageVersionId = FindVersionOrThrow(result, packageToInstall, version);
                }
                else
                {
                    _log.Information($"Install version not specified. Falling back to default install version {packageToInstall.DefaultInstallVersion.Version}");
                }

                _log.Information($"Initiating install of package {packageId}");
                var installResult = await packageManager.InstallPackageAsync(packageToInstall, installOptions);
                var extendedErrorCode = installResult.ExtendedErrorCode?.HResult ?? HRESULT.S_OK;

                // Contract version 4
                var installErrorCode = installResult.GetValueOrDefault(res => res.InstallerErrorCode, HRESULT.S_OK);

                _log.Information($"Install finished. Status={installResult.Status}, InstallerErrorCode={installErrorCode}, ExtendedErrorCode={extendedErrorCode}, RebootRequired={installResult.RebootRequired}");
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
                _log.Error(e, "Elevated app install failed.");
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

    /// <summary>
    /// Find a specific version in the list of available versions for a package.
    /// </summary>
    /// <param name="package">Target package</param>
    /// <param name="version">Version to find</param>
    /// <returns>Specified version</returns>
    /// <exception>Exception thrown if the specified version was not found</exception>
    private PackageVersionId FindVersionOrThrow(ElevatedInstallTaskResult result, CatalogPackage package, string version)
    {
        // Find the version in the list of available versions
        for (var i = 0; i < package.AvailableVersions.Count; i++)
        {
            if (package.AvailableVersions[i].Version == version)
            {
                return package.AvailableVersions[i];
            }
        }

        var installErrorInvalidParameter = unchecked((int)0x8A150112);
        result.Status = (int)InstallResultStatus.InvalidOptions;
        result.ExtendedErrorCode = installErrorInvalidParameter;
        var message = $"Specified install version was not found {version}.";
        _log.Error(message);
        throw new ArgumentException(message);
    }
}
