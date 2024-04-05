// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconCache
{
    Task<BitmapImage> GetIconFromCacheAsync(WidgetDefinition widgetDefinition);
}
