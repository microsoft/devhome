// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.ViewModels;

public partial class AddWidgetViewModel : ObservableObject
{
    private readonly IWidgetHostingService _widgetHostingService;
    private readonly IWidgetScreenshotService _widgetScreenshotService;
    private readonly IWidgetIconService _widgetIconService;
    private readonly IThemeSelectorService _themeSelectorService;

    private ObservableCollection<MenuItemViewModel> ProviderMenuItems { get; set; }

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
        IWidgetHostingService widgetHostingService,
        IWidgetScreenshotService widgetScreenshotService,
        IWidgetIconService widgetIconService,
        IThemeSelectorService themeSelectorService)
    {
        _widgetHostingService = widgetHostingService;
        _widgetScreenshotService = widgetScreenshotService;
        _widgetIconService = widgetIconService;
        _themeSelectorService = themeSelectorService;

        ProviderMenuItems = new Lazy<ObservableCollection<MenuItemViewModel>>(async () =>
        {
            await FillAvailableWidgetsAsync();
            return [];
        });
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

    private async Task<ObservableCollection<MenuItemViewModel>> FillAvailableWidgetsAsync()
    {
        ObservableCollection<MenuItemViewModel> providerMenuItems = [];

        var catalog = await _widgetHostingService.GetWidgetCatalogAsync();
        var host = await _widgetHostingService.GetWidgetHostAsync();

        if (catalog is null || host is null)
        {
            // We should never have gotten here if we don't have a WidgetCatalog.
            Log.Logger()?.ReportError("AddWidgetDialog", $"Opened the AddWidgetDialog, but WidgetCatalog is null.");
            return null;
        }

        // Show the providers and widgets underneath them in alphabetical order.
        var providerDefinitions = await Task.Run(() => catalog!.GetProviderDefinitions().OrderBy(x => x.DisplayName));
        var widgetDefinitions = await Task.Run(() => catalog!.GetWidgetDefinitions().OrderBy(x => x.DisplayTitle));

        Log.Logger()?.ReportInfo("AddWidgetDialog", $"Filling available widget list, found {providerDefinitions.Count()} providers and {widgetDefinitions.Count()} widgets");

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        var currentlyPinnedWidgets = await Task.Run(() => host.GetWidgets());
        foreach (var providerDef in providerDefinitions)
        {
            if (await WidgetHelpers.IsIncludedWidgetProviderAsync(providerDef))
            {
                var navItem = new MenuItemViewModel
                {
                    Tag = providerDef,
                    Text = providerDef.DisplayName,
                };

                foreach (var widgetDef in widgetDefinitions)
                {
                    if (widgetDef.ProviderDefinition.Id.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var image = await _widgetIconService.GetWidgetIconForThemeAsync(widgetDef, _themeSelectorService.GetActualTheme());
                        var enable = !IsSingleInstanceAndAlreadyPinned(widgetDef, currentlyPinnedWidgets);
                        var subItem = new MenuItemViewModel
                        {
                            Tag = widgetDef,
                            Image = image,
                            Text = widgetDef.DisplayTitle,
                            IsEnabled = enable,
                        };

                        navItem.SubMenuItems.Add(subItem);
                    }
                }

                if (navItem.SubMenuItems.Count > 0)
                {
                    providerMenuItems.Add(navItem);
                }
            }
        }

        // If there were no available widgets, log an error.
        // This should never happen since Dev Home's core widgets are always available.
        if (!providerMenuItems.Any())
        {
            Log.Logger()?.ReportError("AddWidgetDialog", $"FillAvailableWidgetsAsync found no available widgets.");
        }

        return providerMenuItems;
    }

    private bool IsSingleInstanceAndAlreadyPinned(WidgetDefinition widgetDef, Widget[] currentlyPinnedWidgets)
    {
        // If a WidgetDefinition has AllowMultiple = false, only one of that widget can be pinned at one time.
        if (!widgetDef.AllowMultiple)
        {
            if (currentlyPinnedWidgets != null)
            {
                foreach (var pinnedWidget in currentlyPinnedWidgets)
                {
                    if (pinnedWidget.DefinitionId == widgetDef.Id)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
