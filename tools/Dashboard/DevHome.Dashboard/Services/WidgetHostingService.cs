// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.Windows.Widgets.Hosts;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Log = DevHome.Dashboard.Helpers.Log;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly IPackageDeploymentService _packageDeploymentService;

    private static readonly string WidgetServiceStorePackageId = "9N3RK8ZV2ZR8";
    private static readonly TimeSpan StoreInstallTimeout = new (0, 0, 60);

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

    public WidgetHostingService(IPackageDeploymentService packageDeploymentService)
    {
        _packageDeploymentService = packageDeploymentService;
    }

    public async Task<bool> EnsureWidgetServiceAsync()
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
            // If we're on Windows 10, check if we have the store version installed. Check against what's really
            // installed instead of the enum, just in case something changed between startup and now.
            if (HasValidWidgetServicePackage())
            {
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 10, HasStoreWidgetServiceGoodVersion");
                _widgetServiceState = WidgetServiceStates.HasStoreWidgetServiceGoodVersion;
                return true;
            }
            else if (_widgetServiceState == WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion)
            {
                // If it's not there and we already knew that, it means we tried to install during setup and it failed.
                // Don't try again when we get to the Dashboard, it takes too long.
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 10, already HasStoreWidgetServiceNoOrBadVersion");
                return false;
            }
            else
            {
                // Try to install and report the outcome.
                Log.Logger()?.ReportInfo("WidgetHostingService", "On Windows 10, TryInstallWidgetServicePackageAsync...");
                var installedSuccessfully = await TryInstallWidgetServicePackageAsync();
                _widgetServiceState = installedSuccessfully ? WidgetServiceStates.HasStoreWidgetServiceGoodVersion : WidgetServiceStates.HasStoreWidgetServiceNoOrBadVersion;
                Log.Logger()?.ReportInfo("WidgetHostingService", $"On Windows 10, ...{_widgetServiceState}");
                return installedSuccessfully;
            }
        }
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

    private async Task<bool> TryInstallWidgetServicePackageAsync()
    {
        try
        {
            var installTask = InstallWidgetServicePackageAsync(WidgetServiceStorePackageId);

            // Wait for a maximum of StoreInstallTimeout (60 seconds).
            var completedTask = await Task.WhenAny(installTask, Task.Delay(StoreInstallTimeout));

            if (completedTask.Exception != null)
            {
                throw completedTask.Exception;
            }

            if (completedTask != installTask)
            {
                throw new TimeoutException("Store Install task did not finish in time.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("WidgetService installation Failed", ex);
        }

        return false;
    }

    private async Task InstallWidgetServicePackageAsync(string packageId)
    {
        await Task.Run(() =>
        {
            var tcs = new TaskCompletionSource<bool>();
            AppInstallItem installItem;
            try
            {
                Log.Logger()?.ReportInfo("WidgetHostingService", "Starting WidgetService install");
                installItem = new AppInstallManager().StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportInfo("WidgetHostingService", "WidgetService install failure");
                tcs.SetException(ex);
                return tcs.Task;
            }

            installItem.Completed += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            installItem.StatusChanged += (sender, args) =>
            {
                if (installItem.GetCurrentStatus().InstallState == AppInstallState.Canceled
                    || installItem.GetCurrentStatus().InstallState == AppInstallState.Error)
                {
                    tcs.TrySetException(new System.Management.Automation.JobFailedException(installItem.GetCurrentStatus().ErrorCode.ToString()));
                }
                else if (installItem.GetCurrentStatus().InstallState == AppInstallState.Completed)
                {
                    tcs.SetResult(true);
                }
            };
            return tcs.Task;
        });
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
