// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Views;

public partial class DashboardView : ToolPage
{
    public override string ShortName => "Dashboard";

    public DashboardViewModel ViewModel { get; }

    public static ObservableCollection<WidgetViewModel> PinnedWidgets { get; set; }

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;
    private AdaptiveCardRenderer _renderer;
    private Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    public DashboardView()
    {
        ViewModel = new DashboardViewModel();
        this.InitializeComponent();
        InitializeWidgetHost();

        PinnedWidgets = new ObservableCollection<WidgetViewModel>();

        Loaded += RestorePinnedWidgets;
    }

    private void InitializeWidgetHost()
    {
        // The GUID is this app's Host GUID that Widget Platform will use to identify this host.
        _widgetHost = WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031"));
        _widgetCatalog = WidgetCatalog.GetDefault();
        _renderer = new AdaptiveCardRenderer();
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        _widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
    }

    private void RestorePinnedWidgets(object sender, RoutedEventArgs e)
    {
        var pinnedWidgets = _widgetHost.GetWidgets();
        if (pinnedWidgets != null)
        {
            foreach (var widget in pinnedWidgets)
            {
                AddWidgetToPinnedWidgets(widget);
            }
        }
    }

    private async void AddWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddWidgetDialog(_widgetHost, _widgetCatalog, _renderer, _dispatcher)
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = this.XamlRoot,
        };
        _ = await dialog.ShowAsync();

        var newWidget = dialog.AddedWidget;

        if (newWidget != null)
        {
            AddWidgetToPinnedWidgets(newWidget);
        }
    }

    private async void AddWidgetToPinnedWidgets(Widget widget)
    {
        var size = await widget.GetSizeAsync();
        var wvm = new WidgetViewModel(widget, size, _renderer, _dispatcher);
        PinnedWidgets.Add(wvm);
    }

    private void OpenWidgetMenu(object sender, RoutedEventArgs e)
    {
        if (sender as Button is Button widgetMenuButton)
        {
            var widgetMenuFlyout = widgetMenuButton.Flyout as MenuFlyout;
            widgetMenuFlyout.Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft;
            if (widgetMenuFlyout?.Items.Count == 0)
            {
                if (widgetMenuButton?.Tag is WidgetViewModel widgetViewModel)
                {
                    var resourceLoader = new ResourceLoader("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
                    var text = resourceLoader.GetString("RemoveWidgetMenuText");
                    var menuItemClose = new MenuFlyoutItem
                    {
                        Tag = widgetViewModel,
                        Text = text,
                    };
                    menuItemClose.Click += DeleteWidgetClick;
                    widgetMenuFlyout.Items.Add(menuItemClose);
                }
            }
        }
    }

    private async void DeleteWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem)
        {
            if (deleteMenuItem?.Tag is WidgetViewModel widgetViewModel)
            {
                // Remove the widget from the list before deleting, otherwise the widget will
                // have changed and the collection won't be able to find it to remove it.
                PinnedWidgets.Remove(widgetViewModel);
                await widgetViewModel.Widget.DeleteAsync();
            }
        }
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            foreach (var widgetToRemove in PinnedWidgets.Where(x => x.Widget.DefinitionId == args.DefinitionId).ToList())
            {
                PinnedWidgets.Remove(widgetToRemove);
            }
        });
    }
}
