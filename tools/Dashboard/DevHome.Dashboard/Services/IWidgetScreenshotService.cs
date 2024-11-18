// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Services;

public interface IWidgetScreenshotService
{
    public void RemoveScreenshotsFromCache(string definitionId);

    public Task<Brush> GetBrushForWidgetScreenshotAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme theme);
}
