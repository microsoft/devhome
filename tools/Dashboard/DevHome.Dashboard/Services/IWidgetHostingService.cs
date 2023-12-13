// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetHostingService
{
    public bool HasValidWebExperiencePack();

    public Task<WidgetHost> GetWidgetHostAsync();

    public Task<WidgetCatalog> GetWidgetCatalogAsync();
}
