// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.ViewModels;

public partial class AddWidgetViewModel : ObservableObject
{
    private readonly IWidgetScreenshotService _widgetScreenshotService;

    [ObservableProperty]
    private string _widgetDisplayTitle;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle;

    [ObservableProperty]
    private Brush _widgetScreenshot;

    [ObservableProperty]
    private bool _pinButtonVisibility;

    public AddWidgetViewModel(IWidgetScreenshotService widgetScreenshotService)
    {
        _widgetScreenshotService = widgetScreenshotService;
    }

    public async Task SetWidgetDefinition(WidgetDefinition selectedWidgetDefinition, ElementTheme actualTheme)
    {
        var bitmap = await _widgetScreenshotService.GetScreenshotFromCache(selectedWidgetDefinition, actualTheme);

        WidgetDisplayTitle = selectedWidgetDefinition.DisplayTitle;
        WidgetProviderDisplayTitle = selectedWidgetDefinition.ProviderDefinition.DisplayName;
        WidgetScreenshot = new ImageBrush
        {
            ImageSource = bitmap,
        };
        PinButtonVisibility = true;
    }

    public void Clear()
    {
        WidgetDisplayTitle = string.Empty;
        WidgetProviderDisplayTitle = string.Empty;
        WidgetScreenshot = null;
        PinButtonVisibility = false;
    }
}
