// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetHostingService
{
    public Task<WidgetHost> GetWidgetHostAsync();

    public Task<WidgetCatalog> GetWidgetCatalogAsync();
}
