// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using WinUIEx;

namespace DevHome.Dashboard.Views;

public sealed partial class AddWidgetDialog : ContentDialog
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AddWidgetDialog));

    private WidgetDefinition _selectedWidget;

    public WidgetDefinition AddedWidget { get; private set; }

    public AddWidgetViewModel ViewModel { get; set; }

    private readonly IWidgetHostingService _hostingService;
    private readonly IWidgetIconService _widgetIconService;
    private readonly WindowEx _windowEx;

    public AddWidgetDialog()
    {
        ViewModel = Application.Current.GetService<AddWidgetViewModel>();
        _hostingService = Application.Current.GetService<IWidgetHostingService>();
        _widgetIconService = Application.Current.GetService<IWidgetIconService>();

        this.InitializeComponent();

        _windowEx = Application.Current.GetService<WindowEx>();

        RequestedTheme = Application.Current.GetService<IThemeSelectorService>().Theme;
    }

    [RelayCommand]
    public async Task OnLoadedAsync()
    {
        var widgetCatalog = await _hostingService.GetWidgetCatalogAsync();
        widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;

        await FillAvailableWidgetsAsync();
        SelectFirstWidgetByDefault();
    }

    private async Task FillAvailableWidgetsAsync()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        // Show the providers and widgets underneath them in alphabetical order.
        var providerDefinitions = (await _hostingService.GetProviderDefinitionsAsync()).OrderBy(x => x.DisplayName);
        var widgetDefinitions = (await _hostingService.GetWidgetDefinitionsAsync()).OrderBy(x => x.DisplayTitle);

        _log.Information($"Filling available widget list, found {providerDefinitions.Count()} providers and {widgetDefinitions.Count()} widgets");

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        var currentlyPinnedWidgets = await _hostingService.GetWidgetsAsync();
        foreach (var providerDef in providerDefinitions)
        {
            if (await WidgetHelpers.IsIncludedWidgetProviderAsync(providerDef))
            {
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = providerDef,
                    Content = providerDef.DisplayName,
                };

                foreach (var widgetDef in widgetDefinitions)
                {
                    if (widgetDef.ProviderDefinition.Id.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var subItemContent = await BuildWidgetNavItem(widgetDef);
                        var enable = !IsSingleInstanceAndAlreadyPinned(widgetDef, currentlyPinnedWidgets);
                        var subItem = new NavigationViewItem
                        {
                            Tag = widgetDef,
                            Content = subItemContent,
                            IsEnabled = enable,
                        };
                        subItem.SetValue(AutomationProperties.AutomationIdProperty, $"NavViewItem_{widgetDef.Id}");
                        subItem.SetValue(AutomationProperties.NameProperty, widgetDef.DisplayTitle);

                        navItem.MenuItems.Add(subItem);
                    }
                }

                if (navItem.MenuItems.Count > 0)
                {
                    AddWidgetNavigationView.MenuItems.Add(navItem);
                }
            }
        }

        // If there were no available widgets, log an error.
        // This should never happen since Dev Home's core widgets are always available.
        if (!AddWidgetNavigationView.MenuItems.Any())
        {
            _log.Error($"FillAvailableWidgetsAsync found no available widgets.");
        }
    }

    private async Task<StackPanel> BuildWidgetNavItem(WidgetDefinition widgetDefinition)
    {
        var image = await _widgetIconService.GetIconFromCacheAsync(widgetDefinition, ActualTheme);
        return BuildNavItem(image, widgetDefinition.DisplayTitle);
    }

    private StackPanel BuildNavItem(BitmapImage image, string text)
    {
        var itemContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
        };

        if (image is not null)
        {
            var itemSquare = new Rectangle()
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 10, 0),
                Fill = new ImageBrush
                {
                    ImageSource = image,
                    Stretch = Stretch.Uniform,
                },
            };

            itemContent.Children.Add(itemSquare);
        }

        var itemText = new TextBlock()
        {
            Text = text,
        };
        itemContent.Children.Add(itemText);

        return itemContent;
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

    private void SelectFirstWidgetByDefault()
    {
        if (AddWidgetNavigationView.MenuItems.Count > 0)
        {
            var firstProvider = AddWidgetNavigationView.MenuItems[0] as NavigationViewItem;
            if (firstProvider.MenuItems.Count > 0)
            {
                var firstWidget = firstProvider.MenuItems[0] as NavigationViewItem;
                AddWidgetNavigationView.SelectedItem = firstWidget;
            }
        }
    }

    private async void AddWidgetNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        // Selected item could be null if list of widgets became empty, but list should never be empty
        // since core widgets are always available.
        if (sender.SelectedItem is null)
        {
            ViewModel.Clear();
            return;
        }

        // Get selected widget definition.
        var selectedTag = (sender.SelectedItem as NavigationViewItem).Tag;
        if (selectedTag is null)
        {
            _log.Error($"Selected widget description did not have a tag");
            ViewModel.Clear();
            return;
        }

        // If the user has selected a widget, show preview. If they selected a provider, leave space blank.
        if (selectedTag as WidgetDefinition is WidgetDefinition selectedWidgetDefinition)
        {
            _selectedWidget = selectedWidgetDefinition;
            await ViewModel.SetWidgetDefinition(selectedWidgetDefinition);
        }
        else if (selectedTag as WidgetProviderDefinition is not null)
        {
            ViewModel.Clear();
        }
    }

    [RelayCommand]
    private async Task UpdateThemeAsync()
    {
        // Update the icons for each available widget listed.
        foreach (var providerItem in AddWidgetNavigationView.MenuItems.OfType<NavigationViewItem>())
        {
            foreach (var widgetItem in providerItem.MenuItems.OfType<NavigationViewItem>())
            {
                if (widgetItem.Tag is WidgetDefinition widgetDefinition)
                {
                    var image = await _widgetIconService.GetIconFromCacheAsync(widgetDefinition, ActualTheme);
                    widgetItem.Content = BuildNavItem(image, widgetDefinition.DisplayTitle);
                }
            }
        }
    }

    [RelayCommand]
    private void PinButtonClick()
    {
        _log.Debug($"Pin selected");
        AddedWidget = _selectedWidget;

        HideDialogAsync();
    }

    [RelayCommand]
    private void CancelButtonClick()
    {
        _log.Debug($"Canceled dialog");
        AddedWidget = null;

        HideDialogAsync();
    }

    private async void HideDialogAsync()
    {
        _selectedWidget = null;
        ViewModel = null;

        var widgetCatalog = await _hostingService.GetWidgetCatalogAsync();
        widgetCatalog!.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;

        this.Hide();
    }

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            // If we currently have the deleted widget open, un-select it.
            if (_selectedWidget is not null &&
                _selectedWidget.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
            {
                _log.Information($"Widget definition deleted while selected.");
                ViewModel.Clear();
                AddWidgetNavigationView.SelectedItem = null;
            }

            // Remove the deleted WidgetDefinition from the list of available widgets.
            var menuItems = AddWidgetNavigationView.MenuItems;
            foreach (var providerItem in menuItems.OfType<NavigationViewItem>())
            {
                foreach (var widgetItem in providerItem.MenuItems.OfType<NavigationViewItem>())
                {
                    if (widgetItem.Tag is WidgetDefinition tagDefinition)
                    {
                        if (tagDefinition.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
                        {
                            providerItem.MenuItems.Remove(widgetItem);

                            // If we've removed all widgets from a provider, remove the provider from the list.
                            if (!providerItem.MenuItems.Any())
                            {
                                menuItems.Remove(providerItem);

                                // If we've removed all providers from the list, log an error.
                                // This should never happen since Dev Home's core widgets are always available.
                                if (!menuItems.Any())
                                {
                                    _log.Error($"WidgetCatalog_WidgetDefinitionDeleted found no available widgets.");
                                }
                            }

                            return;
                        }
                    }
                }
            }
        });
    }

    private void ContentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var contentDialogMaxHeight = (double)Resources["ContentDialogMaxHeight"];
        const int SmallThreshold = 324;
        const int MediumThreshold = 360;

        var smallPinButtonMargin = (Thickness)Resources["SmallPinButtonMargin"];
        var largePinButtonMargin = (Thickness)Resources["LargePinButtonMargin"];
        var smallWidgetPreviewTopMargin = (Thickness)Resources["SmallWidgetPreviewTopMargin"];
        var largeWidgetPreviewTopMargin = (Thickness)Resources["LargeWidgetPreviewTopMargin"];

        AddWidgetNavigationView.Height = Math.Min(this.ActualHeight, contentDialogMaxHeight) - AddWidgetTitleBar.ActualHeight;

        var previewHeightAvailable = AddWidgetNavigationView.Height - TitleRow.ActualHeight - PinRow.ActualHeight;

        // Adjust margins when the height gets too small to show everything.
        if (previewHeightAvailable < SmallThreshold)
        {
            PreviewRow.Padding = smallWidgetPreviewTopMargin;
            PinButton.Margin = smallPinButtonMargin;
        }
        else if (previewHeightAvailable < MediumThreshold)
        {
            PreviewRow.Padding = smallWidgetPreviewTopMargin;
            PinButton.Margin = largePinButtonMargin;
        }
        else
        {
            PreviewRow.Padding = largeWidgetPreviewTopMargin;
            PinButton.Margin = largePinButtonMargin;
        }
    }
}
