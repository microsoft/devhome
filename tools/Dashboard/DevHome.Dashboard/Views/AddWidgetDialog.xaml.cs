// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Widgets.Hosts;
using WinUIEx;

namespace DevHome.Dashboard.Views;
public sealed partial class AddWidgetDialog : ContentDialog
{
    private static WindowEx _mainWindow;
    private readonly WidgetHost _widgetHost;
    private readonly WidgetCatalog _widgetCatalog;
    private Widget _currentWidget;

    public Widget AddedWidget { get; set; }

    public WidgetViewModel ViewModel { get; set; }

    public AddWidgetDialog(
        WidgetHost host,
        WidgetCatalog catalog,
        AdaptiveCardRenderer renderer,
        DispatcherQueue dispatcher,
        ElementTheme theme)
    {
        ViewModel = new WidgetViewModel(null, Microsoft.Windows.Widgets.WidgetSize.Large, null, renderer, dispatcher);
        this.InitializeComponent();

        _widgetHost = host;
        _widgetCatalog = catalog;

        // Strange behavior: setting the requested theme in the constructor isn't enough, so do it here.
        RequestedTheme = theme;

        // Get the application root window so we know when it has closed.
        _mainWindow = Application.Current.GetService<WindowEx>();
        _mainWindow.Closed += OnMainWindowClosed;

        FillAvailableWidgets();
        SelectFirstWidgetByDefault();
    }

    private void FillAvailableWidgets()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        if (_widgetCatalog is null)
        {
            // We should never have gotten here if we don't have a WidgetCatalog.
            Log.Logger()?.ReportError("AddWidgetDialog", $"Opened the AddWidgetDialog, but WidgetCatalog is null.");
            return;
        }

        var providerDefs = _widgetCatalog.GetProviderDefinitions();
        var widgetDefs = _widgetCatalog.GetWidgetDefinitions();

        Log.Logger()?.ReportInfo("AddWidgetDialog", $"Filling available widget list, found {providerDefs.Length} providers and {widgetDefs.Length} widgets");

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        foreach (var providerDef in providerDefs)
        {
            if (WidgetHelpers.IsIncludedWidgetProvider(providerDef))
            {
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = providerDef,
                    Content = providerDef.DisplayName,
                };

                foreach (var widgetDef in widgetDefs)
                {
                    if (widgetDef.ProviderDefinition.Id.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var subItemContent = BuildWidgetNavItem(widgetDef);
                        var enable = !IsSingleInstanceAndAlreadyPinned(widgetDef);
                        var subItem = new NavigationViewItem
                        {
                            Tag = widgetDef,
                            Content = subItemContent,
                            IsEnabled = enable,
                        };

                        navItem.MenuItems.Add(subItem);
                    }
                }

                if (navItem.MenuItems.Count > 0)
                {
                    AddWidgetNavigationView.MenuItems.Add(navItem);
                }
            }
        }
    }

    private StackPanel BuildWidgetNavItem(WidgetDefinition widgetDefinition)
    {
        var image = DashboardView.GetWidgetIconForTheme(widgetDefinition, ActualTheme);
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

    private bool IsSingleInstanceAndAlreadyPinned(WidgetDefinition widgetDef)
    {
        // If a WidgetDefinition has AllowMultiple = false, only one of that widget can be pinned at one time.
        if (!widgetDef.AllowMultiple)
        {
            var currentlyPinnedWidgets = _widgetHost.GetWidgets();
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
        // Delete previously shown configuation widget.
        // Clearing the UI here results in a flash, so don't bother. It will update soon.
        Log.Logger()?.ReportDebug("AddWidgetDialog", $"Widget selection changed, delete widget if one exists");
        var clearWidgetTask = ClearCurrentWidget();

        // Load selected widget configuration.
        var selectedTag = (sender.SelectedItem as NavigationViewItem).Tag;
        if (selectedTag is null)
        {
            Log.Logger()?.ReportError("AddWidgetDialog", $"Selected widget description did not have a tag");
            return;
        }

        // If the user has selected a widget, show configuration UI. If they selected a provider, leave space blank.
        if (selectedTag as WidgetDefinition is WidgetDefinition selectedWidgetDefinition)
        {
            var size = WidgetHelpers.GetLargetstCapabilitySize(selectedWidgetDefinition.GetWidgetCapabilities());

            // Create the widget for configuration. We will need to delete it if the user closes the dialog
            // without pinning, or selects a different widget.
            var widget = await _widgetHost.CreateWidgetAsync(selectedWidgetDefinition.Id, size);
            Log.Logger()?.ReportInfo("AddWidgetDialog", $"Created Widget {widget.Id}");

            ViewModel.Widget = widget;
            PinButton.Visibility = Visibility.Visible;

            clearWidgetTask.Wait();
            _currentWidget = widget;
        }
        else if (selectedTag as WidgetProviderDefinition is not null)
        {
            // Null out the view model background so we don't bind to the old one
            ViewModel.WidgetBackground = null;
            ConfigurationContentFrame.Content = null;
            PinButton.Visibility = Visibility.Collapsed;
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        AddedWidget = _currentWidget;
        ViewModel = null;

        this.Hide();
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Delete previously shown configuation card.
        Log.Logger()?.ReportDebug("AddWidgetDialog", $"Exiting dialog, delete widget");
        await ClearCurrentWidget();

        this.Hide();
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        Log.Logger()?.ReportInfo("AddWidgetDialog", $"Window Closed, delete partially created widget");
        await ClearCurrentWidget();
    }

    private async Task ClearCurrentWidget()
    {
        if (_currentWidget != null)
        {
            var widgetIdToDelete = _currentWidget.Id;
            await _currentWidget.DeleteAsync();
            Log.Logger()?.ReportInfo("AddWidgetDialog", $"Deleted Widget {widgetIdToDelete}");
            _currentWidget = null;
        }
    }
}
