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
    private readonly AdaptiveCardRenderer _renderer;
    private Widget _currentWidget;

    public Widget AddedWidget { get; set; }

    public WidgetViewModel ViewModel { get; set; }

    public AddWidgetDialog(WidgetHost host, WidgetCatalog catalog, AdaptiveCardRenderer renderer, DispatcherQueue dispatcher)
    {
        ViewModel = new WidgetViewModel(null, Microsoft.Windows.Widgets.WidgetSize.Large, renderer, dispatcher);
        this.InitializeComponent();

        _widgetHost = host;
        _widgetCatalog = catalog;
        _renderer = renderer;

        FillAvailableWidgets();
    }

    private void FillAvailableWidgets()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        foreach (var provider in _widgetCatalog.GetProviderDefinitions())
        {
            if (IsIncludedWidgetProvider(provider))
            {
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = provider,
                    Content = provider.DisplayName,
                };

                foreach (var widget in _widgetCatalog.GetWidgetDefinitions())
                {
                    if (widget.ProviderDefinition.Id.Equals(provider.Id, StringComparison.Ordinal))
                    {
                        var subItem = new NavigationViewItem
                        {
                            Tag = widget,
                            Content = widget.DisplayTitle,
                        };
                        navItem.MenuItems.Add(subItem);
                    }
                }

                AddWidgetNavigationView.MenuItems.Add(navItem);
            }
        }
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
        var clearWidgetTask = ClearCurrentWidget();
        ConfigurationContentFrame.Content = null;
        PinButton.Visibility = Visibility.Collapsed;

        // Load selected widget configuration.
        var selectedTag = (sender.SelectedItem as NavigationViewItem).Tag;
        if (selectedTag == null)
        {
            return;
        }

        // If the user has selected a widget, show configuration UI. If they selected a provider, leave space blank.
        var selectedAsWidget = selectedTag as WidgetDefinition;
        if (selectedAsWidget != null)
        {
            var size = WidgetHelpers.GetLargetstCapabilitySize(selectedAsWidget.GetWidgetCapabilities());

            // Create the widget for configuration. We will need to delete it if the user closes the dialog
            // without pinning, or selects a different widget.
            var widget = await _widgetHost.CreateWidgetAsync(selectedAsWidget.Id, size);

            // TODO CreateWidgetAsync doesn't always seem to be "done", and returns blank templates and data.
            // Put in small wait to avoid this.
            System.Threading.Thread.Sleep(100);

            ViewModel.Widget = widget;
            PinButton.Visibility = Visibility.Visible;

            clearWidgetTask.Wait();
            _currentWidget = widget;
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        AddedWidget = _currentWidget;

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
