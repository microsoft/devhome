// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

internal interface IWidgetImplFactory
{
    public WidgetImpl Create(WidgetContext widgetContext, string state);
}
