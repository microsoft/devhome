// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.Dashboard.Helpers;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

    public WidgetHostingService()
    {
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
