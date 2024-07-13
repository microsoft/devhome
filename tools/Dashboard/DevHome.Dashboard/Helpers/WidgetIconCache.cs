// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Storage.Streams;

namespace DevHome.Dashboard.Helpers;
internal class WidgetIconCache
{
    private static Dictionary<string, BitmapImage> _widgetLightIconCache;
    private static Dictionary<string, BitmapImage> _widgetDarkIconCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetIconCache"/> class.
    /// </summary>
    /// <remarks>
    /// The WidgetIconCache is backed by two dictionaries, one for light themed icons and one for dark themed icons.
    /// </remarks>
    public WidgetIconCache()
    {
        _widgetLightIconCache = new Dictionary<string, BitmapImage>();
        _widgetDarkIconCache = new Dictionary<string, BitmapImage>();
    }

    /// <summary>
    /// Caches icons for all widgets in the WidgetCatalog that are included in Dev Home.
    /// </summary>
    public static async Task CacheAllWidgetIcons(WidgetCatalog widgetCatalog, DispatcherQueue dispatcher)
    {
        var widgetDefs = widgetCatalog.GetWidgetDefinitions();
        foreach (var widgetDef in widgetDefs ?? Array.Empty<WidgetDefinition>())
        {
            await AddIconsToCache(widgetDef, dispatcher);
        }
    }

    /// <summary>
    /// Caches two icons for each widget, one for light theme and one for dark theme.
    /// </summary>
    public static async Task AddIconsToCache(WidgetDefinition widgetDef, DispatcherQueue dispatcher)
    {
        // Only cache icons for providers that we're including.
        if (WidgetHelpers.IsIncludedWidgetProvider(widgetDef.ProviderDefinition))
        {
            var widgetDefId = widgetDef.Id;
            try
            {
                Log.Logger()?.ReportDebug("WidgetIconCache", $"Cache widget icons for {widgetDefId}");
                var itemLightImage = await WidgetIconToBitmapImage(widgetDef.GetThemeResource(WidgetTheme.Light).Icon, dispatcher);
                var itemDarkImage = await WidgetIconToBitmapImage(widgetDef.GetThemeResource(WidgetTheme.Dark).Icon, dispatcher);

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
                Log.Logger()?.ReportError("WidgetIconCache", $"Exception in AddIconsToCache:", ex);
                _widgetLightIconCache.Add(widgetDefId, null);
                _widgetDarkIconCache.Add(widgetDefId, null);
            }
        }
    }

    public static void RemoveIconsFromCache(string definitionId)
    {
        _widgetLightIconCache.Remove(definitionId);
        _widgetDarkIconCache.Remove(definitionId);
    }

    public static BitmapImage GetWidgetIconForTheme(WidgetDefinition widgetDefinition, ElementTheme theme)
    {
        BitmapImage image;
        if (theme == ElementTheme.Light)
        {
            _widgetLightIconCache.TryGetValue(widgetDefinition.Id, out image);
        }
        else
        {
            _widgetDarkIconCache.TryGetValue(widgetDefinition.Id, out image);
        }

        return image;
    }

    public static Brush GetBrushForWidgetIcon(WidgetDefinition widgetDefinition, ElementTheme theme)
    {
        var image = GetWidgetIconForTheme(widgetDefinition, theme);

        var brush = new ImageBrush
        {
            ImageSource = image,
        };

        return brush;
    }

    private static async Task<BitmapImage> WidgetIconToBitmapImage(IRandomAccessStreamReference iconStreamRef, DispatcherQueue dispatcher)
    {
        var itemImage = await dispatcher.EnqueueAsync(async () =>
        {
            using var bitmapStream = await iconStreamRef.OpenReadAsync();
            var itemImage = new BitmapImage();
            await itemImage.SetSourceAsync(bitmapStream);
            return itemImage;
        });

        return itemImage;
    }
}
