// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconService
{
    public void RemoveIconsFromCache(string definitionId);

    public Task<BitmapImage> GetIconFromCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme theme);

    public Task<Brush> GetBrushForWidgetIconAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme theme);
}
