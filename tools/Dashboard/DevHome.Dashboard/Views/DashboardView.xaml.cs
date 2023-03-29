// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        PinnedWidgets.CollectionChanged += OnPinnedWidgetsCollectionChanged;

        ActualThemeChanged += OnActualThemeChanged;
        Loaded += RestorePinnedWidgets;
#if DEBUG
        Loaded += AddResetButton;
#endif
    }

    private void InitializeWidgetHost()
    {
        Log.Logger()?.ReportInfo("DashboardView", "Register with WidgetHost");

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
            Log.Logger()?.ReportInfo("DashboardView", $"Get HostConfig file '{hostConfigFileName}'");
            var uri = new Uri($"ms-appx:///DevHome.Dashboard/Assets/{hostConfigFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            hostConfigContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError("DashboardView", "Error retrieving HostConfig", e);
        }

        _dispatcher.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(hostConfigContents))
            {
                _renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
            }
            else
            {
                Log.Logger()?.ReportError("DashboardView", $"HostConfig contents are {hostConfigContents}");
            }
        });

        return;
    }

    private async void RestorePinnedWidgets(object sender, RoutedEventArgs e)
    {
        // TODO: Ideally there would be some sort of visual loading indicator while the renderer gets set up.
        SetHostConfigOnWidgetRenderer().Wait();

        Log.Logger()?.ReportInfo("DashboardView", "Get widgets for current host");
        var pinnedWidgets = _widgetHost.GetWidgets();
        if (pinnedWidgets != null)
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Found {pinnedWidgets.Length} widgets for this host");

            foreach (var widget in pinnedWidgets)
            {
                var size = await widget.GetSizeAsync();
                AddWidgetToPinnedWidgets(widget, size);
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
        Log.Logger()?.ReportDebug("DashboardView", $"Add widget to pinned widgets, id = {widget.Id}");
        InsertWidgetInPinnedWidgets(widget, size, PinnedWidgets.Count);
    }

    private void InsertWidgetInPinnedWidgets(Widget widget, WidgetSize size, int index)
    {
        var wvm = new WidgetViewModel(widget, size, _renderer, _dispatcher);
        var widgetDefinition = _widgetCatalog.GetWidgetDefinition(widget.DefinitionId);
        if (widgetDefinition != null)
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Insert widget in pinned widgets, id = {widget.Id}, index = {index}");
            wvm.WidgetDisplayName = widgetDefinition.DisplayTitle;
        }
        else
        {
            Log.Logger()?.ReportWarn("DashboardView", $"WidgetPlatform did not clean up widget defintion '{widget.DefinitionId}'");
        }

        PinnedWidgets.Insert(index, wvm);
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Log.Logger()?.ReportInfo("DashboardView", $"WidgetDefinitionDeleted {args.DefinitionId}");
            foreach (var widgetToRemove in PinnedWidgets.Where(x => x.Widget.DefinitionId == args.DefinitionId).ToList())
            {
                Log.Logger()?.ReportInfo("DashboardView", $"Remove widget {widgetToRemove.Widget.Id}");
                PinnedWidgets.Remove(widgetToRemove);
            }
        });
    }

    // Listen for widgets being added or removed, so we can add or remove listeners on the WidgetViewModels' properties.
    private void OnPinnedWidgetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
            {
                item.PropertyChanged -= PinnedWidgetsPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
            {
                item.PropertyChanged += PinnedWidgetsPropertyChanged;
            }
        }
    }

    private async void PinnedWidgetsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName.Equals(nameof(WidgetViewModel.IsInEditMode), StringComparison.Ordinal))
        {
            var widgetViewModel = sender as WidgetViewModel;
            if (widgetViewModel.IsInEditMode == true)
            {
                // If the WidgetControl has marked this widget as in edit mode, bring up the edit widget dialog.
                await EditWidget(widgetViewModel);
            }
        }
    }

    // We can't truly edit a widget once it has been pinned. Instead, simulate editing by
    // removing the old widget and creating a new one.
    private async Task EditWidget(WidgetViewModel widgetViewModel)
    {
        // Get info about the widget we're "editing".
        var index = PinnedWidgets.IndexOf(widgetViewModel);
        var originalSize = widgetViewModel.WidgetSize;
        var widgetDef = _widgetCatalog.GetWidgetDefinition(widgetViewModel.Widget.DefinitionId);

        var dialog = new CustomizeWidgetDialog(_widgetHost, _renderer, _dispatcher, widgetDef)
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = this.XamlRoot,
            RequestedTheme = this.ActualTheme,
        };
        _ = await dialog.ShowAsync();

        var newWidget = dialog.EditedWidget;

        if (newWidget != null)
        {
            // Remove and delete the old widget.
            PinnedWidgets.RemoveAt(index);
            await widgetViewModel.Widget.DeleteAsync();

            // Set the original size on the new widget and add it to the list.
            await newWidget.SetSizeAsync(originalSize);
            InsertWidgetInPinnedWidgets(newWidget, originalSize, index);
        }

        widgetViewModel.IsInEditMode = false;
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
