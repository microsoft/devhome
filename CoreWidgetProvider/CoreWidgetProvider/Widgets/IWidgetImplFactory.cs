// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;
internal interface IWidgetImplFactory
{
    public WidgetImpl Create(WidgetContext widgetContext, string state);
}
