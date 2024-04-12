// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetHostingService));

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

    // RPC error codes to recover from
    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    /// <summary>
    /// Get the list of current widgets from the WidgetService.
    /// </summary>
    /// <returns>A list of widgets, or null if there were no widgets or the list could not be retrieved.</returns>
    public async Task<Widget[]> GetWidgetsAsync()
    {
        // If we already have a WidgetHost, check if the OOP COM object is still alive and use it if it is.
        if (_widgetHost != null)
        {
            try
            {
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                _widgetHost = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widgets from service:");
            }
        }

        // If we lost the object, create a new one. This call will get the WidgetService back up and running.
        if (_widgetHost == null)
        {
            try
            {
                _widgetHost = await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widgets from service:");
            }
        }

        return null;
    }

    /// <summary>
    /// Create and return a new widget.
    /// </summary>
    /// <returns>The new widget, or null if one could ont be created.</returns>
    public async Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize)
    {
        // If we already have a WidgetHost, check if the COM object is still alive and use it if it is.
        if (_widgetHost != null)
        {
            try
            {
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                _widgetHost = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception creating a widget:");
            }
        }

        if (_widgetHost == null)
        {
            try
            {
                _widgetHost = await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
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
    /// <returns>The catalog of widgets, or null if one could not be created.
    /// The returned OOP COM object is not guaranteed to still be alive.</returns>
    public async Task<WidgetCatalog> GetWidgetCatalogAsync()
    {
        // Don't check whether the existing WidgetCatalog COM object is still alive and just return what we have.
        // If the object is dead and we try to subscribe to an event on it, the calling code will handle the error.
        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = await Task.Run(() => WidgetCatalog.GetDefault());
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in WidgetCatalog.GetDefault:");
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
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                _widgetCatalog = null;
            }
        }

        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
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
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                _widgetCatalog = null;
            }
        }

        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinitions());
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
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinition(widgetDefinitionId));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                _widgetCatalog = null;
            }
        }

        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinition(widgetDefinitionId));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return null;
    }
}
