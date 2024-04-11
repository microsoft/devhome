// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetHostingService));

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

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
                _log.Error(ex, "Exception in WidgetHost.Register:");
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
                _log.Error(ex, "Exception in WidgetCatalog.GetDefault:");
            }
        }

        return _widgetCatalog;
    }
}
