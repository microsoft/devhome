// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Views;
public sealed partial class AddWidgetDialog : ContentDialog
{
    private readonly WidgetCatalog _widgetCatalog;

    public AddWidgetDialog(WidgetCatalog catalog)
    {
        this.InitializeComponent();

        _widgetCatalog = catalog;

        FillAvailableWidgets();
    }

    private void FillAvailableWidgets()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider
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

    private void AddWidgetNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        // Load correct adaptive card
    }

    private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        this.Hide();
    }
}
