// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Storage.Streams;
using WinUIEx;

namespace DevHome.Dashboard.Services;

internal sealed class WidgetIconCache : IWidgetIconCache
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetIconService));

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly WindowEx _windowEx;

    private readonly ConcurrentDictionary<string, BitmapImage> _widgetLightIconCache;
    private readonly ConcurrentDictionary<string, BitmapImage> _widgetDarkIconCache;

    public WidgetIconCache(
        IThemeSelectorService themeSelectorService,
        WindowEx windowEx)
    {
        _themeSelectorService = themeSelectorService;
        _windowEx = windowEx;

        _widgetLightIconCache = new ConcurrentDictionary<string, BitmapImage>();
        _widgetDarkIconCache = new ConcurrentDictionary<string, BitmapImage>();
    }

    public async Task<BitmapImage> GetIconFromCacheAsync(WidgetDefinition widgetDefinition)
    {
        var widgetDefinitionId = widgetDefinition.Id;
        var theme = _themeSelectorService.GetActualTheme();
        _log.Information("Getting icon for widget {WidgetDefinitionId} with theme {Theme}", widgetDefinitionId, theme);

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
            _log.Information("Found icon for widget {WidgetDefinitionId} in cache", widgetDefinitionId);
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (theme == ElementTheme.Dark)
        {
            bitmapImage = await WidgetIconToBitmapImageAsync(widgetDefinition.GetThemeResource(WidgetTheme.Dark).Icon);
            _widgetDarkIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await WidgetIconToBitmapImageAsync(widgetDefinition.GetThemeResource(WidgetTheme.Light).Icon);
            _widgetLightIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }

        _log.Information("Added icon for widget {WidgetDefinitionId} to cache", widgetDefinitionId);
        return bitmapImage;
    }

    private async Task<BitmapImage> WidgetIconToBitmapImageAsync(IRandomAccessStreamReference iconStreamRef)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        _log.Information("Converting icon stream to bitmap image");
        var completionSource = new TaskCompletionSource<BitmapImage>();
        _windowEx.DispatcherQueue.TryEnqueue(async () =>
        {
            using var bitmapStream = await iconStreamRef.OpenReadAsync();
            var itemImage = new BitmapImage();
            await itemImage.SetSourceAsync(bitmapStream);
            completionSource.TrySetResult(itemImage);
        });

        var bitmapImage = await completionSource.Task;

        _log.Information("Converted icon stream to bitmap image");
        return bitmapImage;
    }
}
