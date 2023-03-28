// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Storage;

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

        ActualThemeChanged += OnActualThemeChanged;
        Loaded += RestorePinnedWidgets;
#if DEBUG
        Loaded += AddResetButton;
#endif
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

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        await SetHostConfigOnWidgetRenderer();
    }

    private async Task SetHostConfigOnWidgetRenderer()
    {
        var hostConfigContents = string.Empty;
        var hostConfigFileName = (ActualTheme == ElementTheme.Light) ? "HostConfigLight.json" : "HostConfigDark.json";
        try
        {
            var uri = new Uri($"ms-appx:///DevHome.Dashboard/Assets/{hostConfigFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            hostConfigContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception)
        {
            // TODO: LogError("DashboardView", "Error rettrieving HostConfig", e);
        }

        _dispatcher.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(hostConfigContents))
            {
                _renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
            }
        });

        return;
    }

    private async void RestorePinnedWidgets(object sender, RoutedEventArgs e)
    {
        // TODO: Ideally there would be some sort of visual loading indicator while the renderer gets set up.
        SetHostConfigOnWidgetRenderer().Wait();

        var pinnedWidgets = _widgetHost.GetWidgets();
        if (pinnedWidgets != null)
        {
            foreach (var widget in pinnedWidgets)
            {
                try
                {
                    var size = await widget.GetSizeAsync();

                    AddWidgetToPinnedWidgets(widget, size);
                }
                catch
                {
                    Debug.WriteLine("as");
                }
            }
        }
    }

    private async void AddWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddWidgetDialog(_widgetHost, _widgetCatalog, _renderer, _dispatcher)
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = this.XamlRoot,
            RequestedTheme = this.ActualTheme,
        };
        _ = await dialog.ShowAsync();

        var newWidget = dialog.AddedWidget;

        if (newWidget != null)
        {
            var widgetDef = _widgetCatalog.GetWidgetDefinition(newWidget.DefinitionId);
            var size = WidgetHelpers.GetDefaultWidgetSize(widgetDef.GetWidgetCapabilities());
            await newWidget.SetSizeAsync(size);
            AddWidgetToPinnedWidgets(newWidget, size);
        }
    }

    private void AddWidgetToPinnedWidgets(Widget widget, WidgetSize size)
    {
        var wvm = new WidgetViewModel(widget, size, _renderer, _dispatcher);
        var widgetDefinition = _widgetCatalog.GetWidgetDefinition(widget.DefinitionId);
        if (widgetDefinition != null)
        {
            wvm.WidgetDisplayName = widgetDefinition.DisplayTitle;
        }
        else
        {
            // TODO: LogWarning("DashboardView", $"WidgetPlatform did not clean up widget defintion {widget.DefinitionId}");
        }

        PinnedWidgets.Add(wvm);
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

#if DEBUG
    private void AddResetButton(object sender, RoutedEventArgs e)
    {
        var resetButton = new Button
        {
            Content = new SymbolIcon(Symbol.Refresh),
            HorizontalAlignment = HorizontalAlignment.Right,
            FontSize = 4,
        };
        resetButton.Click += ResetButton_Click;
        var parent = AddWidgetButton.Parent as StackPanel;
        var index = parent.Children.IndexOf(AddWidgetButton);
        parent.Children.Insert(index + 1, resetButton);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var roamingProperties = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
        if (roamingProperties.ContainsKey("HideDashboardBanner"))
        {
            roamingProperties.Remove("HideDashboardBanner");
        }

        ViewModel.ShowDashboardBanner = true;
    }
#endif
}
