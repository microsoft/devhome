// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconService
{
    public Task CacheAllWidgetIconsAsync();

    public Task AddIconsToCacheAsync(WidgetDefinition widgetDef);

    public void RemoveIconsFromCache(string definitionId);

    public Task<BitmapImage> GetWidgetIconForThemeAsync(WidgetDefinition widgetDefinition, ElementTheme theme);

    public Task<Brush> GetBrushForWidgetIconAsync(WidgetDefinition widgetDefinition, ElementTheme theme);
}
