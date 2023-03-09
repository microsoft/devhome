// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Views;
public sealed partial class AddWidgetDialog : ContentDialog
{
    private readonly WidgetHost _widgetHost;
    private readonly WidgetCatalog _widgetCatalog;
    private readonly AdaptiveCardRenderer _renderer;
    private Widget _currentWidget;

    public AddWidgetDialog(WidgetHost host, WidgetCatalog catalog, AdaptiveCardRenderer renderer)
    {
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
        // Delete previously shown configuation widget
        var clearWidgetTask = ClearCurrentWidget();
        ConfigurationContentFrame.Content = null;

        // Load selected widget configuration
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

            var temp = await widget.GetCardTemplateAsync();
            var data = await widget.GetCardDataAsync();

            // Use the data to fill in the template
            var template = new AdaptiveCardTemplate(temp);
            var json = template.Expand(data);

            // Create the Adaptive Card from the widget
            var card = AdaptiveCard.FromJsonString(json);
            var renderedCard = _renderer.RenderAdaptiveCard(card.AdaptiveCard);
            if (renderedCard.FrameworkElement != null)
            {
                // Add FrameworkElement to the UI
                ConfigurationContentFrame.Content = renderedCard.FrameworkElement;
            }

            clearWidgetTask.Wait();
            _currentWidget = widget;
        }
    }

    private async void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Delete previously shown configuation card
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
