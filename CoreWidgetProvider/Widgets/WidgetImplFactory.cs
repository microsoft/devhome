// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CoreWidgetProvider.Helpers;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;
internal class WidgetImplFactory<T> : IWidgetImplFactory
    where T : WidgetImpl, new()
{
    public WidgetImpl Create(WidgetContext widgetContext, string state)
    {
        Log.Logger()?.ReportDebug($"In WidgetImpl Create for Id {widgetContext.Id} Definition: {widgetContext.DefinitionId} and state: '{state}'");
        WidgetImpl widgetImpl = new T();
        widgetImpl.CreateWidget(widgetContext, state);
        return widgetImpl;
    }
}
