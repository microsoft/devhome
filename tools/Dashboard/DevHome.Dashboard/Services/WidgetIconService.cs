// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Storage.Streams;
using WinUIEx;

namespace DevHome.Dashboard.Services;

public class WidgetIconService : IWidgetIconService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetIconService));

    private readonly WindowEx _windowEx;

    private readonly ConcurrentDictionary<string, BitmapImage> _widgetLightIconCache;
    private readonly ConcurrentDictionary<string, BitmapImage> _widgetDarkIconCache;

    public WidgetIconService(WindowEx windowEx)
    {
        _windowEx = windowEx;

        _widgetLightIconCache = new ConcurrentDictionary<string, BitmapImage>();
        _widgetDarkIconCache = new ConcurrentDictionary<string, BitmapImage>();
    }

    public void RemoveIconsFromCache(string definitionId)
    {
        _widgetLightIconCache.TryRemove(definitionId, out _);
        _widgetDarkIconCache.TryRemove(definitionId, out _);
    }

    public async Task<BitmapImage> GetIconFromCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme theme)
    {
        var widgetDefinitionId = widgetDefinition.Id;
        BitmapImage bitmapImage;

        // First, check the cache to see if the icon is already there.
        if (theme == ElementTheme.Dark)
        {
            _widgetDarkIconCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }
        else
        {
            _widgetLightIconCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (theme == ElementTheme.Dark)
        {
            bitmapImage = await WidgetIconToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Dark)).Icon);
            _widgetDarkIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await WidgetIconToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Light)).Icon);
            _widgetLightIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }

        return bitmapImage;
    }

    public async Task<Brush> GetBrushForWidgetIconAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme theme)
    {
        var image = await GetIconFromCacheAsync(widgetDefinition, theme);

        var brush = new ImageBrush
        {
            ImageSource = image,
        };

        return brush;
    }

    private async Task<BitmapImage> WidgetIconToBitmapImageAsync(IRandomAccessStreamReference iconStreamRef)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        var completionSource = new TaskCompletionSource<BitmapImage>();
        _windowEx.DispatcherQueue.TryEnqueue(async () =>
        {
            using var bitmapStream = await iconStreamRef.OpenReadAsync();
            var itemImage = new BitmapImage();
            await itemImage.SetSourceAsync(bitmapStream);
            completionSource.TrySetResult(itemImage);
        });

        var bitmapImage = await completionSource.Task;

        return bitmapImage;
    }
}
