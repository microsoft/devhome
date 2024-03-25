// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.ViewModels;

public partial class AddWidgetViewModel : ObservableObject
{
    private readonly IWidgetScreenshotService _widgetScreenshotService;
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private string _widgetDisplayTitle;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle;

    [ObservableProperty]
    private Brush _widgetScreenshot;

    [ObservableProperty]
    private bool _pinButtonVisibility;

    private WidgetDefinition _selectedWidgetDefinition;

    public AddWidgetViewModel(
        IWidgetScreenshotService widgetScreenshotService,
        IThemeSelectorService themeSelectorService)
    {
        _widgetScreenshotService = widgetScreenshotService;
        _themeSelectorService = themeSelectorService;
    }

    public async Task SetWidgetDefinition(WidgetDefinition selectedWidgetDefinition)
    {
        _selectedWidgetDefinition = selectedWidgetDefinition;
        var bitmap = await _widgetScreenshotService.GetScreenshotFromCache(selectedWidgetDefinition, _themeSelectorService.GetActualTheme());

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
        _selectedWidgetDefinition = null;
    }

    [RelayCommand]
    private async Task UpdateThemeAsync()
    {
        if (_selectedWidgetDefinition != null)
        {
            // Update the preview image for the selected widget.
            var theme = _themeSelectorService.GetActualTheme();
            var bitmap = await _widgetScreenshotService.GetScreenshotFromCache(_selectedWidgetDefinition, theme);
            WidgetScreenshot = new ImageBrush
            {
                ImageSource = bitmap,
            };
        }
    }
}
