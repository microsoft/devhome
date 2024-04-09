// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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

    public async Task<Widget[]> GetWidgetsAsync()
    {
        // If we already have a WidgetHost, check if the COM object is still alive and use it if it is.
        if (_widgetHost != null)
        {
            try
            {
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Exception trying to use WidgetHost");
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
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widgets from service:");
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
            catch (Exception ex)
            {
                _log.Warning(ex, "Exception trying to use WidgetHost");
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
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widgets from service:");
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
            catch (Exception ex)
            {
                _log.Warning(ex, "Exception trying to use WidgetCatalog");
                _widgetCatalog = null;
            }
        }

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

    public async Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync()
    {
        // If we already have a WidgetCatalog, check if the COM object is still alive.
        if (_widgetCatalog != null)
        {
            try
            {
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (Exception ex)
            {
                _log.Warning("Exception trying to use WidgetCatalog", ex);
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
            catch (Exception ex)
            {
                _log.Warning("WidgetCatalog was not still alive", ex.Message);
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
            catch (Exception ex)
            {
                _log.Warning("WidgetCatalog was not still alive", ex.Message);
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
