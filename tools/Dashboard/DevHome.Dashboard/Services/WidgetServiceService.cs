// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Services;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetServiceService : IWidgetServiceService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetHostingService));

    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly IAppInstallManagerService _appInstallManagerService;

    private WidgetServiceStates _widgetServiceState = WidgetServiceStates.Unknown;

    public WidgetServiceStates GetWidgetServiceState() => _widgetServiceState;

    public enum WidgetServiceStates
    {
        HasWebExperienceGoodVersion,
        HasWebExperienceNoOrBadVersion,
        HasStoreWidgetServiceGoodVersion,
        HasStoreWidgetServiceNoOrBadVersion,
        Unknown,
    }

    public WidgetServiceService(IPackageDeploymentService packageDeploymentService, IAppInstallManagerService appInstallManagerService)
    {
        _packageDeploymentService = packageDeploymentService;
        _appInstallManagerService = appInstallManagerService;
    }

    public bool CheckForWidgetServiceAsync()
    {
        // If we're on Windows 11, check if we have the right WebExperiencePack version of the WidgetService.
        if (RuntimeHelper.IsOnWindows11)
        {
            if (HasValidWebExperiencePack())
            {
                _log.Information("On Windows 11, HasWebExperienceGoodVersion");
                _widgetServiceState = WidgetServiceStates.HasWebExperienceGoodVersion;
                return true;
            }
            else
            {
                _log.Information("On Windows 11, HasWebExperienceNoOrBadVersion");
                _widgetServiceState = WidgetServiceStates.HasWebExperienceNoOrBadVersion;
                return false;
            }
        }
        else
        {
            // If we're on Windows 10, check if we have the store version installed.
            if (HasValidWidgetServicePackage())
            {
                _log.Information("On Windows 10, HasStoreWidgetServiceGoodVersion");
                _widgetServiceState = WidgetServiceStates.HasStoreWidgetServiceGoodVersion;
                return true;
            }
            else
            {
                _log.Information("On Windows 10, HasStoreWidgetServiceNoOrBadVersion");
                _widgetServiceState = WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion;
                return false;
            }
        }
    }

    public async Task<bool> TryInstallingWidgetService()
    {
        _log.Information("Try installing widget service...");
        var installedSuccessfully = await _appInstallManagerService.TryInstallPackageAsync(WidgetHelpers.WidgetServiceStorePackageId);
        _widgetServiceState = installedSuccessfully ? WidgetServiceStates.HasStoreWidgetServiceGoodVersion : WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion;
        _log.Information($"InstalledSuccessfully == {installedSuccessfully}, {_widgetServiceState}");
        return installedSuccessfully;
    }

    private bool HasValidWebExperiencePack()
    {
        var minSupportedVersion400 = new Version(423, 3800);
        var minSupportedVersion500 = new Version(523, 3300);
        var version500 = new Version(500, 0);

        // Ensure the application is installed, and the version is high enough.
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(
            WidgetHelpers.WebExperiencePackageFamilyName,
            (minSupportedVersion400, version500),
            (minSupportedVersion500, null));
        return packages.Any();
    }

    private bool HasValidWidgetServicePackage()
    {
        var minSupportedVersion = new Version(1, 0, 0, 0);

        var packages = _packageDeploymentService.FindPackagesForCurrentUser(WidgetHelpers.WidgetServicePackageFamilyName, (minSupportedVersion, null));
        return packages.Any();
    }
}
