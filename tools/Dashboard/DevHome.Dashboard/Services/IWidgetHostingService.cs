// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using static DevHome.Dashboard.Services.WidgetHostingService;

namespace DevHome.Dashboard.Services;

public interface IWidgetHostingService
{
<<<<<<< Updated upstream
    public bool CheckForWidgetServiceAsync();

    public Task<bool> TryInstallingWidgetService();

    public WidgetServiceStates GetWidgetServiceState();

    public Task<WidgetHost> GetWidgetHostAsync();
=======
    public Task<Widget[]> GetWidgetsAsync();

    public Task<Widget> GetWidgetAsync(string widgetId);

    public Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize);
>>>>>>> Stashed changes

    public Task<WidgetCatalog> GetWidgetCatalogAsync();

    public Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync();

    public Task<WidgetDefinition[]> GetWidgetDefinitionsAsync();

    public Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId);
}
