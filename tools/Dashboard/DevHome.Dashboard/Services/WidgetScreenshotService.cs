// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Storage.Streams;
using WinUIEx;

namespace DevHome.Dashboard.Services;

public class WidgetScreenshotService : IWidgetScreenshotService
{
    private readonly WindowEx _windowEx;

    private readonly ConcurrentDictionary<string, BitmapImage> _widgetLightScreenshotCache;
    private readonly ConcurrentDictionary<string, BitmapImage> _widgetDarkScreenshotCache;

    public WidgetScreenshotService(WindowEx windowEx)
    {
        _windowEx = windowEx;

        _widgetLightScreenshotCache = new ConcurrentDictionary<string, BitmapImage>();
        _widgetDarkScreenshotCache = new ConcurrentDictionary<string, BitmapImage>();
    }

    public void RemoveScreenshotsFromCache(string definitionId)
    {
        _widgetLightScreenshotCache.Remove(definitionId, out _);
        _widgetDarkScreenshotCache.Remove(definitionId, out _);
    }

    public async Task<BitmapImage> GetScreenshotFromCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        var widgetDefinitionId = widgetDefinition.Id;
        BitmapImage bitmapImage;

        // First, check the cache to see if the screenshot is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _widgetDarkScreenshotCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }
        else
        {
            _widgetLightScreenshotCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the screenshot wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await WidgetScreenshotToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Dark)).GetScreenshots().FirstOrDefault().Image);
            _widgetDarkScreenshotCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await WidgetScreenshotToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Light)).GetScreenshots().FirstOrDefault().Image);
            _widgetLightScreenshotCache.TryAdd(widgetDefinitionId, bitmapImage);
        }

        return bitmapImage;
    }

    private async Task<BitmapImage> WidgetScreenshotToBitmapImageAsync(IRandomAccessStreamReference iconStreamRef)
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
