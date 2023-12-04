// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.Dashboard.Helpers;
using Microsoft.Windows.Widgets.Hosts;
using WinUIEx;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly WindowEx _windowEx;

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

    public WidgetHostingService(WindowEx windowEx)
    {
        _windowEx = windowEx;
        RegisterWidgetHost();
        GetDefaultCatalog();
    }

    private void RegisterWidgetHost()
    {
        try
        {
            _windowEx.DispatcherQueue.TryEnqueue(() =>
            {
                _widgetHost = WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031"));
            });
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("WidgetHostingService", "Exception in WidgetHost.Register:", ex);
        }
    }

    public WidgetHost GetWidgetHost()
    {
        return _widgetHost;
    }

    private void GetDefaultCatalog()
    {
        try
        {
            _windowEx.DispatcherQueue.TryEnqueue(() =>
            {
                _widgetCatalog = WidgetCatalog.GetDefault();
            });
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("WidgetHostingService", "Exception in WidgetCatalog.GetDefault:", ex);
        }
    }

    public WidgetCatalog GetWidgetCatalog()
    {
        return _widgetCatalog;
    }
}
