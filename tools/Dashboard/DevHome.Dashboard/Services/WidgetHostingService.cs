// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.Dashboard.Helpers;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

internal class WidgetHostingService
{
    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

    public WidgetHostingService()
    {
    }

    public WidgetHost GetWidgetHost()
    {
        if (_widgetHost == null)
        {
            try
            {
                _widgetHost = WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031"));
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("WidgetHostingService", "Exception in WidgetHost.Register:", ex);
            }
        }

        return _widgetHost;
    }

    public WidgetCatalog GetWidgetCatalog()
    {
        if (_widgetCatalog == null)
        {
            try
            {
                _widgetCatalog = WidgetCatalog.GetDefault();
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("WidgetHostingService", "Exception in WidgetCatalog.GetDefault:", ex);
            }
        }

        return _widgetCatalog;
    }
}
