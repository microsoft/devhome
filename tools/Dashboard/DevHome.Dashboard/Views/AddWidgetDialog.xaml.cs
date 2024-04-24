// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Views;

public sealed partial class AddWidgetDialog : ContentDialog
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AddWidgetDialog));

    private ComSafeWidgetDefinition _selectedWidget;

    public ComSafeWidgetDefinition AddedWidget { get; private set; }

    public AddWidgetViewModel ViewModel { get; set; }

    private readonly IWidgetHostingService _hostingService;
    private readonly IWidgetIconService _widgetIconService;
    private readonly DispatcherQueue _dispatcherQueue;

    public AddWidgetDialog()
    {
        ViewModel = Application.Current.GetService<AddWidgetViewModel>();
        _hostingService = Application.Current.GetService<IWidgetHostingService>();
        _widgetIconService = Application.Current.GetService<IWidgetIconService>();

        this.InitializeComponent();

        _dispatcherQueue = Application.Current.GetService<DispatcherQueue>();

        RequestedTheme = Application.Current.GetService<IThemeSelectorService>().Theme;
    }

    [RelayCommand]
    public async Task OnLoadedAsync()
    {
        try
        {
            var widgetCatalog = await _hostingService.GetWidgetCatalogAsync();
            widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
        }
        catch (Exception ex)
        {
            // If there was an error getting the widget catalog, log it and continue.
            // If a WidgetDefinition is deleted while the dialog is open, we won't know to remove it from
            // the list automatically, but we can show a helpful error message if the user tries to pin it.
            // https://github.com/microsoft/devhome/issues/2623
            _log.Error(ex, "Exception in AddWidgetDialog.OnLoadedAsync:");
        }

        await FillAvailableWidgetsAsync();
        SelectFirstWidgetByDefault();
    }

    private async Task FillAvailableWidgetsAsync()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        // Show the providers and widgets underneath them in alphabetical order.
        var comSafeProviderDefinitions = await ComSafeHelpers.GetAllOrderedComSafeProviderDefinitions(_hostingService);
        var comSafeWidgetDefinitions = await ComSafeHelpers.GetAllOrderedComSafeWidgetDefinitions(_hostingService);

        _log.Information($"Filling available widget list, found {comSafeProviderDefinitions.Count} providers and {comSafeWidgetDefinitions.Count} widgets");

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        var unsafeCurrentlyPinnedWidgets = await _hostingService.GetWidgetsAsync();
        var comSafeCurrentlyPinnedWidgets = new List<ComSafeWidget>();
        foreach (var unsafeWidget in unsafeCurrentlyPinnedWidgets)
        {
            var id = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (!string.IsNullOrEmpty(id))
            {
                var comSafeWidget = new ComSafeWidget(id);
                if (await comSafeWidget.PopulateAsync())
                {
                    comSafeCurrentlyPinnedWidgets.Add(comSafeWidget);
                }
            }
        }

        foreach (var providerDef in comSafeProviderDefinitions)
        {
            if (await WidgetHelpers.IsIncludedWidgetProviderAsync(providerDef))
            {
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = providerDef,
                    Content = new TextBlock { Text = providerDef.DisplayName, TextWrapping = TextWrapping.Wrap },
                };

                navItem.SetValue(ToolTipService.ToolTipProperty, providerDef.DisplayName);

                foreach (var widgetDef in comSafeWidgetDefinitions)
                {
                    if (widgetDef.ProviderDefinition.Id.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var subItemContent = await BuildWidgetNavItem(widgetDef);
                        var enable = !IsSingleInstanceAndAlreadyPinned(widgetDef, [.. comSafeCurrentlyPinnedWidgets]);
                        var subItem = new NavigationViewItem
                        {
                            Tag = widgetDef,
                            Content = subItemContent,
                            IsEnabled = enable,
                        };
                        subItem.SetValue(AutomationProperties.AutomationIdProperty, $"NavViewItem_{widgetDef.Id}");
                        subItem.SetValue(AutomationProperties.NameProperty, widgetDef.DisplayTitle);
                        subItem.SetValue(ToolTipService.ToolTipProperty, widgetDef.DisplayTitle);

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

    private async Task<Grid> BuildWidgetNavItem(ComSafeWidgetDefinition widgetDefinition)
    {
        var image = await _widgetIconService.GetIconFromCacheAsync(widgetDefinition, ActualTheme);
        return BuildNavItem(image, widgetDefinition.DisplayTitle);
    }

    private Grid BuildNavItem(BitmapImage image, string text)
    {
        var itemContent = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
        };

        if (image is not null)
        {
            var itemSquare = new Rectangle()
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 8, 0),
                Fill = new ImageBrush
                {
                    ImageSource = image,
                    Stretch = Stretch.Uniform,
                },
            };
            Grid.SetColumn(itemSquare, 0);

            itemContent.Children.Add(itemSquare);
        }

        var itemText = new TextBlock()
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(itemText, 1);

        itemContent.Children.Add(itemText);

        return itemContent;
    }

    private bool IsSingleInstanceAndAlreadyPinned(ComSafeWidgetDefinition widgetDef, ComSafeWidget[] currentlyPinnedWidgets)
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
        if (selectedTag as ComSafeWidgetDefinition is ComSafeWidgetDefinition selectedWidgetDefinition)
        {
            _selectedWidget = selectedWidgetDefinition;
            await ViewModel.SetWidgetDefinition(selectedWidgetDefinition);
        }
        else if (selectedTag as ComSafeWidgetProviderDefinition is not null)
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
                if (widgetItem.Tag is ComSafeWidgetDefinition widgetDefinition)
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

        try
        {
            var widgetCatalog = await _hostingService.GetWidgetCatalogAsync();
            widgetCatalog.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
        }
        catch (Exception ex)
        {
            // If there was an error getting the widget catalog, log it and continue.
            _log.Error(ex, "Exception in HideDialogAsync:");
        }

        this.Hide();
    }

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        _dispatcherQueue.TryEnqueue(() =>
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
                    if (widgetItem.Tag is ComSafeWidgetDefinition tagDefinition)
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
