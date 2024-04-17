// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
<<<<<<< Updated upstream
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Services;
=======
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
>>>>>>> Stashed changes
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetHostingService));

    private readonly IPackageDeploymentService _packageDeploymentService;
    private readonly IAppInstallManagerService _appInstallManagerService;

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

<<<<<<< Updated upstream
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

    public async Task<WidgetHost> GetWidgetHostAsync()
=======
    // RPC error codes to recover from
    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private const int MaxAttempts = 3;

    /// <summary>
    /// Get the list of current widgets from the WidgetService.
    /// </summary>
    /// <returns>A list of widgets, or null if there were no widgets or the list could not be retrieved.</returns>
    public async Task<Widget[]> GetWidgetsAsync()
>>>>>>> Stashed changes
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetHost before trying again. Also reset the WidgetCatalog,
                // since if we lost the host we probably lost the catalog too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
<<<<<<< Updated upstream
                _log.Error("Exception in WidgetHost.Register:", ex);
=======
                _log.Error(ex, "Exception getting widgets from service:");
>>>>>>> Stashed changes
            }
        }

        return null;
    }

    /// <summary>Gets the widget with the given ID.</summary>
    /// <returns>The widget, or null if one could not be retrieved.</returns>
    public async Task<Widget> GetWidgetAsync(string widgetId)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(() => _widgetHost.GetWidget(widgetId));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
<<<<<<< Updated upstream
                _log.Error("Exception in WidgetCatalog.GetDefault:", ex);
=======
                _log.Error(ex, $"Exception getting widget with id {widgetId}:");
            }
        }

        return null;
    }

    /// <summary>
    /// Create and return a new widget.
    /// </summary>
    /// <returns>The new widget, or null if one could not be created.</returns>
    public async Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception creating a widget:");
            }
        }

        return null;
    }

    /// <summary>
    /// Get the catalog of widgets from the WidgetService.
    /// </summary>
    /// <returns>The catalog of widgets, or null if one could not be created.</returns>
    public async Task<WidgetCatalog> GetWidgetCatalogAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());

                // Need to use an arbitrary method to check if the COM object is still alive.
                await Task.Run(() => _widgetCatalog.GetWidgetDefinition("fakeWidgetDefinitionId"));

                // If the above call didn't throw, the object is still alive.
                return _widgetCatalog;
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
                _widgetCatalog = null;
>>>>>>> Stashed changes
            }
        }

        return _widgetCatalog;
    }

    /// <summary>
    /// Get the list of WidgetProviderDefinitions from the WidgetService.
    /// </summary>
    /// <returns>A list of WidgetProviderDefinitions, or an empty list if there were no widgets
    /// or the list could not be retrieved.</returns>
    public async Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return [];
    }

    /// <summary>
    /// Get the list of WidgetDefinitions from the WidgetService.
    /// </summary>
    /// <returns>A list of WidgetDefinitions, or an empty list if there were no widgets
    /// or the list could not be retrieved.</returns>
    public async Task<WidgetDefinition[]> GetWidgetDefinitionsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return [];
    }

    /// <summary>
    /// Get the WidgetDefinition for the given WidgetDefinitionId from the WidgetService.
    /// </summary>
    /// <returns>The WidgetDefinition, or null if the widget definition could not be found
    /// or there was an error retrieving it.</returns>
    public async Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinition(widgetDefinitionId));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return null;
    }
}
