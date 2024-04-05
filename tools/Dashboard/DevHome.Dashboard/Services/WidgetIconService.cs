// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

internal sealed class WidgetIconService : IWidgetIconService
{
    private readonly WidgetIconCache _widgetIconCache;

    public WidgetIconService(WidgetIconCache widgetIconCache)
    {
        _widgetIconCache = widgetIconCache;
    }

    public async Task<BitmapImage> GetWidgetIconAsync(WidgetDefinition widgetDefinition)
    {
        return await _widgetIconCache.GetIconFromCacheAsync(widgetDefinition);
    }

    public async Task<Brush> GetBrushForWidgetIconAsync(WidgetDefinition widgetDefinition)
    {
        var image = await GetWidgetIconAsync(widgetDefinition);

        var brush = new ImageBrush
        {
            ImageSource = image,
        };

        return brush;
    }
}
