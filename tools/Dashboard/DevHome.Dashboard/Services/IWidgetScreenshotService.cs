// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetScreenshotService
{
    public Task<BitmapImage> GetScreenshotFromCache(WidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
