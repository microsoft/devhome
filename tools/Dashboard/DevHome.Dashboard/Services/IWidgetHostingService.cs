// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetHostingService
{
    /// <summary>Get the list of current widgets from the WidgetService.</summary>
    /// <returns>A list of widgets, or empty list if there were no widgets or the list could not be retrieved.</returns>
    public Task<Widget[]> GetWidgetsAsync();

    /// <summary>Gets the widget with the given ID.</summary>
    /// <returns>The widget, or null if one could not be retrieved.</returns>
    public Task<Widget> GetWidgetAsync(string widgetId);

    /// <summary>Create and return a new widget.</summary>
    /// <returns>The new widget, or null if one could not be created.</returns>
    public Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize);

    /// <summary>Get the catalog of widgets from the WidgetService.</summary>
    /// <returns>The catalog of widgets, or null if one could not be created.</returns>
    public Task<WidgetCatalog> GetWidgetCatalogAsync();

    /// <summary>Get the list of WidgetProviderDefinitions from the WidgetService.</summary>
    /// <returns>A list of WidgetProviderDefinitions, or an empty list if there were no widgets
    /// or the list could not be retrieved.</returns>
    public Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync();

    /// <summary>Get the WidgetProviderDefinition for the given WidgetProviderDefinitionId from the WidgetService.</summary>
    /// <returns>The WidgetProviderDefinition, or null if the widget provider definition could not be found
    /// or there was an error retrieving it.</returns>
    public Task<WidgetProviderDefinition> GetProviderDefinitionAsync(string widgetProviderDefinitionId);

    /// <summary>Get the list of WidgetDefinitions from the WidgetService.</summary>
    /// <returns>A list of WidgetDefinitions, or an empty list if there were no widgets
    /// or the list could not be retrieved.</returns>
    public Task<WidgetDefinition[]> GetWidgetDefinitionsAsync();

    /// <summary>Get the WidgetDefinition for the given WidgetDefinitionId from the WidgetService.</summary>
    /// <returns>The WidgetDefinition, or null if the widget definition could not be found
    /// or there was an error retrieving it.</returns>
    public Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId);
}
