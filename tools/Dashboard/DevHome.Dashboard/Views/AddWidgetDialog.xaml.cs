// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
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
using WinUIEx;

namespace DevHome.Dashboard.Views;

public sealed partial class AddWidgetDialog : ContentDialog
{
    private Widget _currentWidget;
    private static DispatcherQueue _dispatcher;

    public Widget AddedWidget { get; set; }

    public WidgetViewModel ViewModel { get; set; }

    private readonly IWidgetHostingService _hostingService;
    private readonly IWidgetIconService _widgetIconService;

    public AddWidgetDialog(
        DispatcherQueue dispatcher,
        ElementTheme theme)
    {
        ViewModel = new WidgetViewModel(null, Microsoft.Windows.Widgets.WidgetSize.Large, null, dispatcher);
        _hostingService = Application.Current.GetService<IWidgetHostingService>();
        _widgetIconService = Application.Current.GetService<IWidgetIconService>();

        this.InitializeComponent();

        _dispatcher = dispatcher;

        // Strange behavior: just setting the requested theme when we new-up the dialog results in
        // the wrong theme's resources being used. Setting RequestedTheme here fixes the problem.
        RequestedTheme = theme;

        // Get the application root window so we know when it has closed.
        Application.Current.GetService<WindowEx>().Closed += OnMainWindowClosed;

        _hostingService.GetWidgetCatalog()!.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
    }

    [RelayCommand]
    public async Task OnLoadedAsync()
    {
        await FillAvailableWidgetsAsync();
        SelectFirstWidgetByDefault();
    }

    private async Task FillAvailableWidgetsAsync()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        if (_hostingService.GetWidgetCatalog() is null)
        {
            // We should never have gotten here if we don't have a WidgetCatalog.
            Log.Logger()?.ReportError("AddWidgetDialog", $"Opened the AddWidgetDialog, but WidgetCatalog is null.");
            return;
        }

        // Show the providers and widgets underneath them in alphabetical order.
        var providerDefinitions = _hostingService.GetWidgetCatalog()!.GetProviderDefinitions().OrderBy(x => x.DisplayName);
        var widgetDefinitions = _hostingService.GetWidgetCatalog()!.GetWidgetDefinitions().OrderBy(x => x.DisplayTitle);

        Log.Logger()?.ReportInfo("AddWidgetDialog", $"Filling available widget list, found {providerDefinitions.Count()} providers and {widgetDefinitions.Count()} widgets");

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
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
                        var enable = !IsSingleInstanceAndAlreadyPinned(widgetDef);
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

        // If there were no available widgets, show a message.
        if (!AddWidgetNavigationView.MenuItems.Any())
        {
            ViewModel.ShowErrorCard("WidgetErrorCardNoWidgetsText");
        }
    }

    private async Task<StackPanel> BuildWidgetNavItem(WidgetDefinition widgetDefinition)
    {
        var image = await _widgetIconService.GetWidgetIconForThemeAsync(widgetDefinition, ActualTheme);
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
            var currentlyPinnedWidgets = _hostingService.GetWidgetHost()?.GetWidgets();
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
        // Delete previously shown configuration widget.
        // Clearing the UI here results in a flash, so don't bother. It will update soon.
        Log.Logger()?.ReportDebug("AddWidgetDialog", $"Widget selection changed, delete widget if one exists");
        var clearWidgetTask = ClearCurrentWidget();

        // Selected item could be null if list of widgets became empty.
        if (sender.SelectedItem is null)
        {
            return;
        }

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
            var size = WidgetHelpers.GetLargestCapabilitySize(selectedWidgetDefinition.GetWidgetCapabilities());

            // Create the widget for configuration. We will need to delete it if the user closes the dialog
            // without pinning, or selects a different widget.
            Widget widget = null;
            try
            {
                widget = await _hostingService.GetWidgetHost()?.CreateWidgetAsync(selectedWidgetDefinition.Id, size);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportWarn("AddWidgetDialog", $"CreateWidgetAsync failed: ", ex);
            }

            if (widget is not null)
            {
                Log.Logger()?.ReportInfo("AddWidgetDialog", $"Created Widget {widget.Id}");

                ViewModel.Widget = new Models.WidgetModel(widget);
                ViewModel.IsInAddMode = true;
                PinButton.Visibility = Visibility.Visible;

                var widgetDefinition = _hostingService.GetWidgetCatalog()!.GetWidgetDefinition(widget.DefinitionId);
                ViewModel.WidgetDefinition = widgetDefinition;

                clearWidgetTask.Wait();
            }
            else
            {
                Log.Logger()?.ReportWarn("AddWidgetDialog", $"Widget creation failed.");
                ViewModel.ShowErrorCard("WidgetErrorCardCreate1Text", "WidgetErrorCardCreate2Text");
            }

            _currentWidget = widget;
        }
        else if (selectedTag as WidgetProviderDefinition is not null)
        {
            ConfigurationContentFrame.Content = null;
            PinButton.Visibility = Visibility.Collapsed;
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        AddedWidget = _currentWidget;

        HideDialog();
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Delete previously shown configuration card.
        Log.Logger()?.ReportDebug("AddWidgetDialog", $"Canceled dialog, delete widget");
        await ClearCurrentWidget();

        HideDialog();
    }

    private void HideDialog()
    {
        _currentWidget = null;
        ViewModel = null;

        Application.Current.GetService<WindowEx>().Closed -= OnMainWindowClosed;
        _hostingService.GetWidgetCatalog()!.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;

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

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        _dispatcher.TryEnqueue(() =>
        {
            // If we currently have the deleted widget open, show an error message instead.
            if (_currentWidget is not null &&
                _currentWidget.DefinitionId.Equals(deletedDefinitionId, StringComparison.Ordinal))
            {
                Log.Logger()?.ReportInfo("AddWidgetDialog", $"Widget definition deleted while creating that widget.");
                ViewModel.ShowErrorCard("WidgetErrorCardCreate1Text", "WidgetErrorCardCreate2Text");
                AddWidgetNavigationView.SelectedItem = null;
            }

            // Remove the deleted WidgetDefinition from the list of available widgets.
            var menuItems = AddWidgetNavigationView.MenuItems;
            foreach (var providerItem in menuItems.Cast<NavigationViewItem>())
            {
                foreach (var widgetItem in providerItem.MenuItems.Cast<NavigationViewItem>())
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

                                // If we've removed all providers from the list, show a message.
                                if (!menuItems.Any())
                                {
                                    ViewModel.ShowErrorCard("WidgetErrorCardNoWidgetsText");
                                }
                            }
                        }
                    }
                }
            }
        });
    }

    private void ContentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        const int ContentDialogMaxHeight = 684;

        AddWidgetNavigationView.Height = Math.Min(this.ActualHeight, ContentDialogMaxHeight) - AddWidgetTitleBar.ActualHeight;

        // Subtract 45 for the margin around ConfigurationContentFrame.
        ConfigurationContentViewer.Height = AddWidgetNavigationView.Height - PinRow.ActualHeight - 45;
    }
}
