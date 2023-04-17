// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DevHome.Dashboard.Views;
public partial class DashboardView : ToolPage
{
    public override string ShortName => "Dashboard";

    public DashboardViewModel ViewModel { get; }

    public static ObservableCollection<WidgetViewModel> PinnedWidgets { get; set; }

    private static WidgetHost _widgetHost;
    private static WidgetCatalog _widgetCatalog;
    private static AdaptiveCardRenderer _renderer;
    private static Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private static bool _widgetHostInitialized;

    private static SortedDictionary<string, BitmapImage> _widgetLightIconCache;
    private static SortedDictionary<string, BitmapImage> _widgetDarkIconCache;
    private static SortedDictionary<string, BitmapImage> _providerIconCache;

    public DashboardView()
    {
        ViewModel = new DashboardViewModel();
        this.InitializeComponent();

        // If this is the first time we're initializing the Dashboard, or if initialization failed last time, initialize now.
        if (!_widgetHostInitialized)
        {
            _widgetHostInitialized = InitializeWidgetHost();
        }
        else
        {
            PinnedWidgets.CollectionChanged -= OnPinnedWidgetsCollectionChanged;
        }

        if (_widgetHostInitialized)
        {
            PinnedWidgets = new ObservableCollection<WidgetViewModel>();
            PinnedWidgets.CollectionChanged += OnPinnedWidgetsCollectionChanged;

            ActualThemeChanged += OnActualThemeChanged;

            Loaded += OnLoaded;
        }
        else
        {
            // TODO: show error
            AddWidgetButton.IsEnabled = false;
        }

#if DEBUG
        Loaded += AddResetButton;
#endif
    }

    private bool InitializeWidgetHost()
    {
        Log.Logger()?.ReportInfo("DashboardView", "Register with WidgetHost");

        try
        {
            // The GUID is this app's Host GUID that Widget Platform will use to identify this host.
            _widgetHost = WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031"));
            _widgetCatalog = WidgetCatalog.GetDefault();
            _renderer = new AdaptiveCardRenderer();
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            _widgetCatalog.WidgetProviderDefinitionAdded += WidgetCatalog_WidgetProviderDefinitionAdded;
            _widgetCatalog.WidgetProviderDefinitionDeleted += WidgetCatalog_WidgetProviderDefinitionDeleted;
            _widgetCatalog.WidgetDefinitionAdded += WidgetCatalog_WidgetDefinitionAdded;
            _widgetCatalog.WidgetDefinitionUpdated += WidgetCatalog_WidgetDefinitionUpdated;
            _widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("DashboardView", "Exception in InitializeWidgetHost:", ex);
            return false;
        }

        return true;
    }

    private async Task<AdaptiveCardRenderer> GetConfigurationRendererAsync()
    {
        // When we render a card in an add or edit dialog, we need to have a different Hostonfig,
        // so create a new renderer for those situations. We can't just temporarily edit the existing
        // renderer, because a pinned widget might get re-rendered the wrong way while the dialog is open.
        var configRenderer = new AdaptiveCardRenderer();
        await SetHostConfigOnWidgetRenderer(configRenderer);
        configRenderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        return configRenderer;
    }

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        // The app uses a different host config to render widgets (adaptive cards) in light and dark themes.
        await SetHostConfigOnWidgetRenderer(_renderer);
    }

    private async Task SetHostConfigOnWidgetRenderer(AdaptiveCardRenderer renderer)
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
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("DashboardView", "Error retrieving HostConfig", ex);
        }

        _dispatcher.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(hostConfigContents))
            {
                renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
            }
            else
            {
                Log.Logger()?.ReportError("DashboardView", $"HostConfig contents are {hostConfigContents}");
            }
        });

        return;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _widgetLightIconCache = new SortedDictionary<string, BitmapImage>();
        _widgetDarkIconCache = new SortedDictionary<string, BitmapImage>();
        _providerIconCache = new SortedDictionary<string, BitmapImage>();

        // Cache the widget icons before we display the widgets, since we include the icons in the widgets.
        await CacheWidgetIcons();
        RestorePinnedWidgets(null, null);
        await CacheProviderIcons();
    }

    private async Task CacheWidgetIcons()
    {
        var widgetDefs = _widgetCatalog.GetWidgetDefinitions();
        foreach (var widgetDef in widgetDefs ?? Array.Empty<WidgetDefinition>())
        {
            await CacheWidgetIcon(widgetDef);
        }
    }

    private async Task CacheWidgetIcon(WidgetDefinition widgetDef)
    {
        // Only cache icons for providers that we're including.
        if (WidgetHelpers.IsIncludedWidgetProvider(widgetDef.ProviderDefinition))
        {
            var widgetDefId = widgetDef.Id;
            try
            {
                Log.Logger()?.ReportDebug("DashboardView", $"Cache widget icon for {widgetDefId}");
                var itemLightImage = await WidgetIconToBitmapImage(widgetDef.GetThemeResource(WidgetTheme.Light).Icon);
                var itemDarkImage = await WidgetIconToBitmapImage(widgetDef.GetThemeResource(WidgetTheme.Dark).Icon);

                // There is a widget bug where Definition update events are being raised as added events.
                // If we already have an icon for this key, just remove and add again.
                if (_widgetLightIconCache.ContainsKey(widgetDefId))
                {
                    _widgetLightIconCache.Remove(widgetDefId);
                }

                if (_widgetDarkIconCache.ContainsKey(widgetDefId))
                {
                    _widgetDarkIconCache.Remove(widgetDefId);
                }

                _widgetLightIconCache.Add(widgetDefId, itemLightImage);
                _widgetDarkIconCache.Add(widgetDefId, itemDarkImage);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("DashboardView", $"Exception in CacheWidgetIcons:", ex);
                _widgetLightIconCache.Add(widgetDefId, null);
                _widgetDarkIconCache.Add(widgetDefId, null);
            }
        }
    }

    private async Task CacheProviderIcons()
    {
        var providerDefs = _widgetCatalog.GetProviderDefinitions();
        foreach (var providerDef in providerDefs)
        {
            await CacheProviderIcon(providerDef);
        }
    }

    private async Task CacheProviderIcon(WidgetProviderDefinition providerDef)
    {
        // Only cache icons for providers that we're including.
        if (WidgetHelpers.IsIncludedWidgetProvider(providerDef))
        {
            var providerDefId = providerDef.Id;
            try
            {
                Log.Logger()?.ReportDebug("DashboardView", $"Cache widget provider icon for {providerDefId}");
                var itemImage = await WidgetIconToBitmapImage(providerDef.Icon);

                // There is a widget bug where Definition update events are being raised as added events.
                // If we already have an icon for this key, just remove and add again.
                if (_providerIconCache.ContainsKey(providerDefId))
                {
                    _providerIconCache.Remove(providerDefId);
                }

                _providerIconCache.Add(providerDefId, itemImage);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("DashboardView", $"Exception in CacheProviderIcon:", ex);
                _providerIconCache.Add(providerDefId, null);
            }
        }
    }

    public static BitmapImage GetProviderIcon(WidgetProviderDefinition widgetProviderDefinition)
    {
        _providerIconCache.TryGetValue(widgetProviderDefinition.Id, out var image);
        return image;
    }

    public static BitmapImage GetWidgetIconForTheme(WidgetDefinition widgetDefinition, ElementTheme theme)
    {
        BitmapImage image;
        if (theme == ElementTheme.Light)
        {
            _widgetLightIconCache.TryGetValue(widgetDefinition.Id, out image);
        }
        else
        {
            _widgetDarkIconCache.TryGetValue(widgetDefinition.Id, out image);
        }

        return image;
    }

    public static Brush GetBrushForWidgetIcon(WidgetDefinition widgetDefinition, ElementTheme theme)
    {
        var image = GetWidgetIconForTheme(widgetDefinition, theme);

        var brush = new Microsoft.UI.Xaml.Media.ImageBrush
        {
            ImageSource = image,
        };
        return brush;
    }

    public static async Task<BitmapImage> WidgetIconToBitmapImage(IRandomAccessStreamReference iconStreamRef)
    {
        using var bitmapStream = await iconStreamRef.OpenReadAsync();
        var itemImage = new BitmapImage();
        await itemImage.SetSourceAsync(bitmapStream);
        return itemImage;
    }

    private async void RestorePinnedWidgets(object sender, RoutedEventArgs e)
    {
        // TODO: Ideally there would be some sort of visual loading indicator while the renderer gets set up.
        await SetHostConfigOnWidgetRenderer(_renderer);

        Log.Logger()?.ReportInfo("DashboardView", "Get widgets for current host");
        var pinnedWidgets = _widgetHost.GetWidgets();
        if (pinnedWidgets != null)
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Found {pinnedWidgets.Length} widgets for this host");

            foreach (var widget in pinnedWidgets)
            {
                try
                {
                    var stateStr = await widget.GetCustomStateAsync();
                    Log.Logger()?.ReportInfo("DashboardView", $"GetWidgetCustomState: {stateStr}");
                    if (!string.IsNullOrEmpty(stateStr))
                    {
                        var stateObj = System.Text.Json.JsonSerializer.Deserialize<WidgetCustomState>(stateStr);
                        if (stateObj.Host == WidgetHelpers.DevHomeHostName)
                        {
                            var size = await widget.GetSizeAsync();
                            AddWidgetToPinnedWidgets(widget, size);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger()?.ReportError("DashboardView", $"RestorePinnedWidgets(): ", ex);
                }
            }
        }
        else
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Found 0 widgets for this host");
        }
    }

    private async void AddWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        var configurationRenderer = await GetConfigurationRendererAsync();
        var dialog = new AddWidgetDialog(_widgetHost, _widgetCatalog, configurationRenderer, _dispatcher, ActualTheme)
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = this.XamlRoot,
            RequestedTheme = this.ActualTheme,
        };
        _ = await dialog.ShowAsync();

        var newWidget = dialog.AddedWidget;

        if (newWidget != null)
        {
            // Set custom state on new widget.
            var newCustomState = WidgetHelpers.CreateWidgetCustomState();
            Log.Logger()?.ReportDebug("DashboardView", $"SetCustomState: {newCustomState}");
            await newWidget.SetCustomStateAsync(newCustomState);

            // Put new widget on the Dashboard.
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
        var widgetDefinition = _widgetCatalog.GetWidgetDefinition(widget.DefinitionId);
        if (widgetDefinition != null)
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Insert widget in pinned widgets, id = {widget.Id}, index = {index}");
            var wvm = new WidgetViewModel(widget, size, widgetDefinition, _renderer, _dispatcher);
            PinnedWidgets.Insert(index, wvm);
        }
        else
        {
            Log.Logger()?.ReportWarn("DashboardView", $"WidgetPlatform did not clean up widgets with defintion '{widget.DefinitionId}'");
        }
    }

    private async void WidgetCatalog_WidgetProviderDefinitionAdded(WidgetCatalog sender, WidgetProviderDefinitionAddedEventArgs args)
    {
        await CacheProviderIcon(args.ProviderDefinition);
    }

    private void WidgetCatalog_WidgetProviderDefinitionDeleted(WidgetCatalog sender, WidgetProviderDefinitionDeletedEventArgs args)
    {
        _providerIconCache.Remove(args.ProviderDefinitionId);
    }

    private async void WidgetCatalog_WidgetDefinitionAdded(WidgetCatalog sender, WidgetDefinitionAddedEventArgs args)
    {
        await CacheWidgetIcon(args.Definition);
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        foreach (var widgetToUpdate in PinnedWidgets.Where(x => x.Widget.DefinitionId == args.Definition.Id).ToList())
        {
            // Things in the definition that we need to update to if they have changed:
            // AllowMultiple, DisplayTitle, Capabilities (size), ThemeResource (icons)
            var oldDef = widgetToUpdate.WidgetDefinition;
            var newDef = args.Definition;

            // If we're no longer allowed to have multiple instances of this widget, delete all of them.
            if (newDef.AllowMultiple == false && oldDef.AllowMultiple == true)
            {
                _dispatcher.TryEnqueue(async () =>
                {
                    Log.Logger()?.ReportInfo("DashboardView", $"No longer allowed to have multiple of widget {newDef.Id}");
                    Log.Logger()?.ReportInfo("DashboardView", $"Delete widget {widgetToUpdate.Widget.Id}");
                    PinnedWidgets.Remove(widgetToUpdate);
                    await widgetToUpdate.Widget.DeleteAsync();
                    Log.Logger()?.ReportInfo("DashboardView", $"Deleted Widget {widgetToUpdate.Widget.Id}");
                });
            }
            else
            {
                // Changing the definition updates the DisplayTitle.
                widgetToUpdate.WidgetDefinition = newDef;

                // If the size the widget is currently set to is no longer supported by the widget, revert to its default size.
                // TODO: Need to update WidgetControl with now-valid sizes.
                // TODO: Properly compare widget capabilities.
                if (oldDef.GetWidgetCapabilities() != newDef.GetWidgetCapabilities())
                {
                    // TODO: handle the case where this change is made while Dev Home is not running -- how do we restore?
                    if (!newDef.GetWidgetCapabilities().Any(cap => cap.Size == widgetToUpdate.WidgetSize))
                    {
                        var newDefaultSize = WidgetHelpers.GetDefaultWidgetSize(newDef.GetWidgetCapabilities());
                        widgetToUpdate.WidgetSize = newDefaultSize;
                        await widgetToUpdate.Widget.SetSizeAsync(newDefaultSize);
                    }
                }
            }

            // TODO: ThemeResource (icons) changed.
        }
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var definitionId = args.DefinitionId;
        _dispatcher.TryEnqueue(() =>
        {
            Log.Logger()?.ReportInfo("DashboardView", $"WidgetDefinitionDeleted {definitionId}");
            foreach (var widgetToRemove in PinnedWidgets.Where(x => x.Widget.DefinitionId == definitionId).ToList())
            {
                Log.Logger()?.ReportInfo("DashboardView", $"Remove widget {widgetToRemove.Widget.Id}");
                PinnedWidgets.Remove(widgetToRemove);
            }
        });

        _widgetLightIconCache.Remove(definitionId);
        _widgetDarkIconCache.Remove(definitionId);
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
                Log.Logger()?.ReportInfo("DashboardView", $"EditWidget {widgetViewModel.Widget.Id}");
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

        var configurationRenderer = await GetConfigurationRendererAsync();
        var dialog = new CustomizeWidgetDialog(_widgetHost, configurationRenderer, _dispatcher, widgetDef)
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
            var state = await widgetViewModel.Widget.GetCustomStateAsync();
            PinnedWidgets.RemoveAt(index);
            await widgetViewModel.Widget.DeleteAsync();

            // Put the old widget's state on the new widget.
            await newWidget.SetCustomStateAsync(state);

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
