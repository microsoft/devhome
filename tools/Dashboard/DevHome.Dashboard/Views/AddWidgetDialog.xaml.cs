// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Views;
public sealed partial class AddWidgetDialog : ContentDialog
{
    private readonly WidgetHost _widgetHost;
    private readonly WidgetCatalog _widgetCatalog;
    private Widget _currentWidget;

    public Widget AddedWidget { get; set; }

    public WidgetViewModel ViewModel { get; set; }

    public AddWidgetDialog(WidgetHost host, WidgetCatalog catalog, AdaptiveCardRenderer renderer, DispatcherQueue dispatcher)
    {
        ViewModel = new WidgetViewModel(null, Microsoft.Windows.Widgets.WidgetSize.Large, renderer, dispatcher);
        this.InitializeComponent();

        _widgetHost = host;
        _widgetCatalog = catalog;

        FillAvailableWidgets();
    }

    private void FillAvailableWidgets()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        foreach (var providerDef in _widgetCatalog.GetProviderDefinitions())
        {
            if (IsIncludedWidgetProvider(providerDef))
            {
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = providerDef,
                    Content = providerDef.DisplayName,
                };

                foreach (var widgetDef in _widgetCatalog.GetWidgetDefinitions())
                {
                    if (widgetDef.ProviderDefinition.Id.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var subItem = new NavigationViewItem
                        {
                            Tag = widgetDef,
                            Content = widgetDef.DisplayTitle,
                        };
                        if (AlreadyPinnedSingleInstance(widgetDef))
                        {
                            subItem.IsEnabled = false;
                        }

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

    private bool AlreadyPinnedSingleInstance(WidgetDefinition widgetDef)
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

    private bool IsIncludedWidgetProvider(WidgetProviderDefinition provider)
    {
        return provider.Id.StartsWith("Microsoft.Windows.DevHome", StringComparison.CurrentCulture);
    }

    private async void AddWidgetNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        // Delete previously shown configuation widget.
        // Clearing the UI here results in a flash, so don't bother. It will update soon.
        var clearWidgetTask = ClearCurrentWidget();

        // Load selected widget configuration.
        var selectedTag = (sender.SelectedItem as NavigationViewItem).Tag;
        if (selectedTag == null)
        {
            return;
        }

        // If the user has selected a widget, show configuration UI. If they selected a provider, leave space blank.
        var selectedWidgetDefinition = selectedTag as WidgetDefinition;
        if (selectedWidgetDefinition != null)
        {
            var size = WidgetHelpers.GetLargetstCapabilitySize(selectedWidgetDefinition.GetWidgetCapabilities());

            // Create the widget for configuration. We will need to delete it if the user closes the dialog
            // without pinning, or selects a different widget.
            var widget = await _widgetHost.CreateWidgetAsync(selectedWidgetDefinition.Id, size);

            // TODO CreateWidgetAsync doesn't always seem to be "done", and returns blank templates and data.
            // Put in small wait to avoid this.
            System.Threading.Thread.Sleep(100);

            ViewModel.Widget = widget;
            PinButton.Visibility = Visibility.Visible;

            clearWidgetTask.Wait();
            _currentWidget = widget;
        }
        else
        {
            var selectedWidgetProviderDefintion = selectedTag as WidgetProviderDefinition;
            if (selectedWidgetProviderDefintion != null)
            {
                // Null out the view model background so we don't bind to the old one
                ViewModel.WidgetBackground = null;
                ConfigurationContentFrame.Content = null;
                PinButton.Visibility = Visibility.Collapsed;
            }
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
        await ClearCurrentWidget();

        this.Hide();
    }

    private async Task ClearCurrentWidget()
    {
        if (_currentWidget != null)
        {
            await _currentWidget.DeleteAsync();
            _currentWidget = null;
        }
    }
}
