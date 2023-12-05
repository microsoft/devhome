// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Storage.Streams;
using WinUIEx;

namespace DevHome.Dashboard.Services;

public class WidgetIconService : IWidgetIconService
{
    private readonly WindowEx _windowEx;

    private readonly IWidgetHostingService _widgetHostingService;

    private readonly Dictionary<string, BitmapImage> _widgetLightIconCache;
    private readonly Dictionary<string, BitmapImage> _widgetDarkIconCache;

    public WidgetIconService(WindowEx windowEx, IWidgetHostingService widgetHostingService)
    {
        _windowEx = windowEx;
        _widgetHostingService = widgetHostingService;

        _widgetLightIconCache = new Dictionary<string, BitmapImage>();
        _widgetDarkIconCache = new Dictionary<string, BitmapImage>();
    }

    /// <summary>
    /// Caches icons for all widgets in the WidgetCatalog that are included in Dev Home.
    /// </summary>
    public async Task CacheAllWidgetIconsAsync()
    {
        var cacheTasks = new List<Task>();
        var widgetCatalog = await _widgetHostingService.GetWidgetCatalogAsync();
        var widgetDefinitions = widgetCatalog?.GetWidgetDefinitions();
        foreach (var widgetDef in widgetDefinitions ?? Array.Empty<WidgetDefinition>())
        {
            var task = AddIconsToCacheAsync(widgetDef);
            cacheTasks.Add(task);
        }

        await Task.WhenAll(cacheTasks);
    }

    /// <summary>
    /// Caches two icons for each widget, one for light theme and one for dark theme.
    /// </summary>
    public async Task AddIconsToCacheAsync(WidgetDefinition widgetDef)
    {
        // Only cache icons for providers that we're including.
        if (await WidgetHelpers.IsIncludedWidgetProviderAsync(widgetDef.ProviderDefinition))
        {
            var widgetDefId = widgetDef.Id;
            try
            {
                Log.Logger()?.ReportDebug("WidgetIconCache", $"Cache widget icons for {widgetDefId}");
                var itemLightImage = await WidgetIconToBitmapImageAsync(widgetDef.GetThemeResource(WidgetTheme.Light).Icon);
                var itemDarkImage = await WidgetIconToBitmapImageAsync(widgetDef.GetThemeResource(WidgetTheme.Dark).Icon);

                // There is a widget bug where Definition update events are being raised as added events.
                // If we already have an icon for this key, just remove and add again in case the icons changed.
                if (_widgetLightIconCache.ContainsKey(widgetDefId))
                {
                    _widgetLightIconCache.Remove(widgetDefId);
                }

                if (_widgetDarkIconCache.ContainsKey(widgetDefId))
                {
                    _widgetDarkIconCache.Remove(widgetDefId);
                }

                _widgetLightIconCache.Add(widgetDefId, itemLightImage);
                _widgetDarkIconCache.Add(widgetDefId, itemDarkImage);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("WidgetIconCache", $"Exception in AddIconsToCacheAsync:", ex);
                _widgetLightIconCache.Add(widgetDefId, null);
                _widgetDarkIconCache.Add(widgetDefId, null);
            }
        }
    }

    public void RemoveIconsFromCache(string definitionId)
    {
        _widgetLightIconCache.Remove(definitionId);
        _widgetDarkIconCache.Remove(definitionId);
    }

    public async Task<BitmapImage> GetWidgetIconForThemeAsync(WidgetDefinition widgetDefinition, ElementTheme theme)
    {
        // Return the WidgetDefinition Id via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        var completionSource = new TaskCompletionSource<string>();
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            completionSource.TrySetResult(widgetDefinition.Id);
        });

        var widgetDefinitionId = await completionSource.Task;

        BitmapImage image;
        if (theme == ElementTheme.Light)
        {
            _widgetLightIconCache.TryGetValue(widgetDefinitionId, out image);
        }
        else
        {
            _widgetDarkIconCache.TryGetValue(widgetDefinitionId, out image);
        }

        return image;
    }

    public async Task<Brush> GetBrushForWidgetIconAsync(WidgetDefinition widgetDefinition, ElementTheme theme)
    {
        var image = await GetWidgetIconForThemeAsync(widgetDefinition, theme);

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
