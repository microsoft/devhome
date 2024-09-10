// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Dashboard.Helpers;
using DevHome.Services.Core.Contracts;
using Serilog;
using Windows.ApplicationModel;

namespace DevHome.Dashboard.Services;

public class WidgetServiceService : IWidgetServiceService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetServiceService));

    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly IMicrosoftStoreService _msStoreService;

    private WidgetServiceStates _widgetServiceState = WidgetServiceStates.Unknown;

    public enum WidgetServiceStates
    {
        MeetsMinVersion,
        NotAtMinVersion,
        NotOK,
        Updating,
        Unknown,
    }

    public WidgetServiceService(IPackageDeploymentService packageDeploymentService, IMicrosoftStoreService msStoreService)
    {
        _packageDeploymentService = packageDeploymentService;
        _msStoreService = msStoreService;
    }

    public WidgetServiceStates GetWidgetServiceState()
    {
        var isWindows11String = RuntimeHelper.IsOnWindows11 ? "Windows 11" : "Windows 10";
        _log.Information($"Checking for WidgetService on {isWindows11String}");

        // First check for the WidgetsPlatformRuntime package. If it's installed and has a valid state, we return that state.
        _log.Information("Checking for WidgetsPlatformRuntime...");
        var package = GetWidgetsPlatformRuntimePackage();
        _widgetServiceState = ValidatePackage(package);
        if (_widgetServiceState == WidgetServiceStates.MeetsMinVersion ||
            _widgetServiceState == WidgetServiceStates.Updating)
        {
            return _widgetServiceState;
        }

        // If the WidgetsPlatformRuntime package is not installed or not high enough version, check for the WebExperience package.
        _log.Information("Checking for WebExperiencePack...");
        package = GetWebExperiencePackPackage();
        _widgetServiceState = ValidatePackage(package);

        return _widgetServiceState;
    }

    public async Task<bool> TryInstallingWidgetService()
    {
        _log.Information("Try installing widget service...");
        var installedSuccessfully = await _msStoreService.TryInstallPackageAsync(WidgetHelpers.WidgetsPlatformRuntimePackageId);
        _widgetServiceState = ValidatePackage(GetWidgetsPlatformRuntimePackage());
        _log.Information($"InstalledSuccessfully == {installedSuccessfully}, {_widgetServiceState}");
        return installedSuccessfully;
    }

    private Package GetWebExperiencePackPackage()
    {
        var minSupportedVersion400 = new Version(423, 3800);
        var minSupportedVersion500 = new Version(523, 3300);
        var version500 = new Version(500, 0);

        // Ensure the application is installed, and the version is high enough.
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(
            WidgetHelpers.WebExperiencePackageFamilyName,
            (minSupportedVersion400, version500),
            (minSupportedVersion500, null));

        if (packages.Any())
        {
            return packages.First();
        }

        return null;
    }

    private Package GetWidgetsPlatformRuntimePackage()
    {
        var minSupportedVersion = new Version(1, 0, 0, 0);

        var packages = _packageDeploymentService.FindPackagesForCurrentUser(WidgetHelpers.WidgetsPlatformRuntimePackageFamilyName, (minSupportedVersion, null));
        if (packages.Any())
        {
            return packages.First();
        }

        return null;
    }

    private WidgetServiceStates ValidatePackage(Package package)
    {
        WidgetServiceStates packageStatus;
        if (package == null)
        {
            packageStatus = WidgetServiceStates.NotAtMinVersion;
        }
        else if (package.Status.VerifyIsOK())
        {
            packageStatus = WidgetServiceStates.MeetsMinVersion;
        }
        else if (package.Status.Servicing == true)
        {
            packageStatus = WidgetServiceStates.Updating;
        }
        else
        {
            packageStatus = WidgetServiceStates.NotOK;
        }

        _log.Information($"ValidatePackage found {packageStatus}");
        return packageStatus;
    }
}
