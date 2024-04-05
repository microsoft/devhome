// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconService
{
    public Task<BitmapImage> GetWidgetIconAsync(WidgetDefinition widgetDefinition);

    public Task<Brush> GetBrushForWidgetIconAsync(WidgetDefinition widgetDefinition);
}
