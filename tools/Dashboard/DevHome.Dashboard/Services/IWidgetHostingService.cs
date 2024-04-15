// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetHostingService
{
    public Task<Widget[]> GetWidgetsAsync();

    public Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize);

    public Task<WidgetCatalog> GetWidgetCatalogAsync();

    public Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync();

    public Task<WidgetDefinition[]> GetWidgetDefinitionsAsync();

    public Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId);
}
