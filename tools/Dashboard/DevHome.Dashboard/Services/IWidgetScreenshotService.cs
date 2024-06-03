// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Dashboard.Services;

public interface IWidgetScreenshotService
{
    public void RemoveScreenshotsFromCache(string definitionId);

    public Task<BitmapImage> GetScreenshotFromCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
