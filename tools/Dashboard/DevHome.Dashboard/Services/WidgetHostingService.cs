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

    public async Task<Widget[]> GetWidgetsAsync()
    {
        // If we already have a WidgetHost, check if the COM object is still alive and use it if it is.
        if (_widgetHost != null)
        {
            try
            {
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                _log.Warning(e, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
                _widgetHost = null;
            }
        }

        if (_widgetHost == null)
        {
            try
            {
                _widgetHost = await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (Exception e)
            {
                _log.Error(e, "Exception getting widgets from service:");
            }
        }

        return [];
    }

    public async Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize)
    {
        // If we already have a WidgetHost, check if the COM object is still alive and use it if it is.
        if (_widgetHost != null)
        {
            try
            {
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                _log.Warning(e, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
                _widgetHost = null;
            }
        }

        if (_widgetHost == null)
        {
            try
            {
                _widgetHost = await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
            }
            catch (Exception e)
            {
                _log.Error(e, "Exception getting widgets from service:");
            }
        }

        return null;
    }

    public async Task<WidgetCatalog> GetWidgetCatalogAsync()
    {
        // If we already have a WidgetCatalog, check if the COM object is still alive and use it if it is.
        if (_widgetCatalog != null)
        {
            try
            {
                await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                _log.Warning(e, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
                _widgetCatalog = null;
            }
        }

        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = await Task.Run(() => WidgetCatalog.GetDefault());
            }
            catch (Exception e)
            {
                _log.Error(e, "Exception in WidgetCatalog.GetDefault:");
            }
        }

        return _widgetCatalog;
    }

    public async Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync()
    {
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                _log.Warning(e, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
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
                _log.Error("Exception in GetWidgetDefinitionAsync:", ex);
            }
        }

        return [];
    }

    public async Task<WidgetDefinition[]> GetWidgetDefinitionsAsync()
    {
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinitions());
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                _log.Warning(e, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
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
                _log.Error("Exception in GetWidgetDefinitionAsync:", ex);
            }
        }

        return [];
    }

    public async Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId)
    {
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinition(widgetDefinitionId));
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                _log.Warning(e, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
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
                _log.Error("Exception in GetWidgetDefinitionAsync:", ex);
            }
        }

        return null;
    }
}
