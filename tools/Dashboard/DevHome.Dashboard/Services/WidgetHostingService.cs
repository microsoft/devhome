// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Services;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly IAppInstallManagerService _appInstallManagerService;

    private static readonly string WidgetServiceStorePackageId = "9N3RK8ZV2ZR8";

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

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

    public WidgetHostingService(IPackageDeploymentService packageDeploymentService, IAppInstallManagerService appInstallManagerService)
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
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 11, HasWebExperienceGoodVersion");
                _widgetServiceState = WidgetServiceStates.HasWebExperienceGoodVersion;
                return true;
            }
            else
            {
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 11, HasWebExperienceNoOrBadVersion");
                _widgetServiceState = WidgetServiceStates.HasWebExperienceNoOrBadVersion;
                return false;
            }
        }
        else
        {
            // If we're on Windows 10, check if we have the store version installed.
            if (HasValidWidgetServicePackage())
            {
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 10, HasStoreWidgetServiceGoodVersion");
                _widgetServiceState = WidgetServiceStates.HasStoreWidgetServiceGoodVersion;
                return true;
            }
            else
            {
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 10, HasStoreWidgetServiceNoOrBadVersion");
                _widgetServiceState = WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion;
                return false;
            }
        }
    }

    public async Task<bool> TryInstallingWidgetService()
    {
        Log.Logger()?.ReportInfo("WidgetHostingService", "Try installing widget service...");
        var installedSuccessfully = await _appInstallManagerService.TryInstallPackageAsync(WidgetServiceStorePackageId);
        _widgetServiceState = installedSuccessfully ? WidgetServiceStates.HasStoreWidgetServiceGoodVersion : WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion;
        Log.Logger()?.ReportInfo("WidgetHostingService", $"InstalledSuccessfully == {installedSuccessfully}, {_widgetServiceState}");
        return installedSuccessfully;
    }

    private bool HasValidWebExperiencePack()
    {
        var minSupportedVersion400 = new Version(423, 3800);
        var minSupportedVersion500 = new Version(523, 3300);
        var version500 = new Version(500, 0);

        // Ensure the application is installed, and the version is high enough.
        const string packageFamilyName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(
            packageFamilyName,
            (minSupportedVersion400, version500),
            (minSupportedVersion500, null));
        return packages.Any();
    }

    private bool HasValidWidgetServicePackage()
    {
        var minSupportedVersion = new Version(1, 0, 0, 0);

        const string packageFamilyName = "Microsoft.WidgetsPlatformRuntime_8wekyb3d8bbwe";
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(packageFamilyName, (minSupportedVersion, null));
        return packages.Any();
    }

    public async Task<WidgetHost> GetWidgetHostAsync()
    {
        if (_widgetHost == null)
        {
            try
            {
                _widgetHost = await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("WidgetHostingService", "Exception in WidgetHost.Register:", ex);
            }
        }

        return _widgetHost;
    }

    public async Task<WidgetCatalog> GetWidgetCatalogAsync()
    {
        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = await Task.Run(() => WidgetCatalog.GetDefault());
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("WidgetHostingService", "Exception in WidgetCatalog.GetDefault:", ex);
            }
        }

        return _widgetCatalog;
    }
}
