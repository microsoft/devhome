// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetHostingService
{
    public WidgetHost GetWidgetHost();

    public WidgetCatalog GetWidgetCatalog();
}
