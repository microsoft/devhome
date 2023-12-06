// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Dashboard.Controls;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.TelemetryEvents;
using DevHome.Dashboard.ViewModels;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Windows.System;

namespace DevHome.Dashboard.Views;

public partial class DashboardView : ToolPage
{
    public override string ShortName => "Dashboard";

    public DashboardViewModel ViewModel
    {
        get;
    }

    internal DashboardBannerViewModel BannerViewModel
    {
        get;
    }

    public static ObservableCollection<WidgetViewModel> PinnedWidgets
    {
        get; set;
    }

    private static Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private static bool _widgetHostInitialized;

    private const string DraggedWidget = "DraggedWidget";
    private const string DraggedIndex = "DraggedIndex";

    public DashboardView()
    {
        ViewModel = Application.Current.GetService<DashboardViewModel>();
        BannerViewModel = Application.Current.GetService<DashboardBannerViewModel>();

        this.InitializeComponent();

        PinnedWidgets = new ObservableCollection<WidgetViewModel>();

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        ActualThemeChanged += OnActualThemeChanged;

        Loaded += OnLoaded;

#if DEBUG
        Loaded += AddResetButton;
#endif
    }

    private async Task<bool> SubscribeToWidgetCatalogEventsAsync()
    {
        Log.Logger()?.ReportInfo("DashboardView", "SubscribeToWidgetCatalogEvents");

        try
        {
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                return false;
            }

            widgetCatalog!.WidgetProviderDefinitionAdded += WidgetCatalog_WidgetProviderDefinitionAdded;
            widgetCatalog!.WidgetProviderDefinitionDeleted += WidgetCatalog_WidgetProviderDefinitionDeleted;
            widgetCatalog!.WidgetDefinitionAdded += WidgetCatalog_WidgetDefinitionAdded;
            widgetCatalog!.WidgetDefinitionUpdated += WidgetCatalog_WidgetDefinitionUpdated;
            widgetCatalog!.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("DashboardView", "Exception in SubscribeToWidgetCatalogEvents:", ex);
            return false;
        }

        return true;
    }

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        // A different host config is used to render widgets (adaptive cards) in light and dark themes.
        await Application.Current.GetService<IAdaptiveCardRenderingService>().UpdateHostConfig();

        // Re-render the widgets with the new theme and renderer.
        foreach (var wvm in PinnedWidgets)
        {
            await wvm.RenderAsync();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeDashboard();
    }

    private async Task<bool> EnsureHostingInitializedAsync()
    {
        if (_widgetHostInitialized)
        {
            return _widgetHostInitialized;
        }

        _widgetHostInitialized = ViewModel.EnsureWebExperiencePack() && await SubscribeToWidgetCatalogEventsAsync();

        return _widgetHostInitialized;
    }

    private async Task<bool> InitializeDashboard()
    {
        LoadingWidgetsProgressRing.Visibility = Visibility.Visible;
        ViewModel.IsLoading = true;

        if (await EnsureHostingInitializedAsync())
        {
            // Cache the widget icons before we display the widgets, since we include the icons in the widgets.
            await ViewModel.WidgetIconService.CacheAllWidgetIconsAsync();

            await RestorePinnedWidgetsAsync();
        }
        else
        {
            Log.Logger()?.ReportWarn("DashboardView", $"Initialization failed");
        }

        LoadingWidgetsProgressRing.Visibility = Visibility.Collapsed;
        ViewModel.IsLoading = false;

        return _widgetHostInitialized;
    }

    private async Task RestorePinnedWidgetsAsync()
    {
        Log.Logger()?.ReportInfo("DashboardView", "Get widgets for current host");
        var widgetHost = await ViewModel.WidgetHostingService.GetWidgetHostAsync();
        var hostWidgets = await Task.Run(() => widgetHost?.GetWidgets());

        if (hostWidgets == null)
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Found 0 widgets for this host");
            return;
        }

        Log.Logger()?.ReportInfo("DashboardView", $"Found {hostWidgets.Length} widgets for this host");
        var restoredWidgetsWithPosition = new SortedDictionary<int, Widget>();
        var restoredWidgetsWithoutPosition = new SortedDictionary<int, Widget>();
        var numUnorderedWidgets = 0;

        // Widgets do not come from the host in a deterministic order, so save their order in each widget's CustomState.
        // Iterate through all the widgets and put them in order. If a widget does not have a position assigned to it,
        // append it at the end. If a position is missing, just show the next widget in order.
        foreach (var widget in hostWidgets)
        {
            try
            {
                var stateStr = await Task.Run(async () => await widget.GetCustomStateAsync());
                Log.Logger()?.ReportInfo("DashboardView", $"GetWidgetCustomState: {stateStr}");

                if (string.IsNullOrEmpty(stateStr))
                {
                    // If we have a widget with no state, Dev Home does not consider it a valid widget
                    // and should delete it, rather than letting it run invisibly in the background.
                    await DeleteAbandonedWidgetAsync(widget, widgetHost);
                    continue;
                }

                var stateObj = System.Text.Json.JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);
                if (stateObj.Host != WidgetHelpers.DevHomeHostName)
                {
                    // This shouldn't be able to be reached
                    Log.Logger()?.ReportError("DashboardView", $"Widget has custom state but no HostName.");
                    continue;
                }

                var position = stateObj.Position;
                if (position >= 0)
                {
                    if (!restoredWidgetsWithPosition.TryAdd(position, widget))
                    {
                        // If there was an error and a widget with this position is already there,
                        // treat this widget as unordered and put it into the unordered map.
                        restoredWidgetsWithoutPosition.Add(numUnorderedWidgets++, widget);
                    }
                }
                else
                {
                    // Widgets with no position will get the default of -1. Append these at the end.
                    restoredWidgetsWithoutPosition.Add(numUnorderedWidgets++, widget);
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("DashboardView", $"RestorePinnedWidgets(): ", ex);
            }
        }

        // Now that we've ordered the widgets, put them in their final collection.
        var finalPlace = 0;
        foreach (var orderedWidget in restoredWidgetsWithPosition)
        {
            await PlaceWidget(orderedWidget, finalPlace++);
        }

        foreach (var orderedWidget in restoredWidgetsWithoutPosition)
        {
            await PlaceWidget(orderedWidget, finalPlace++);
        }
    }

    private async Task DeleteAbandonedWidgetAsync(Widget widget, WidgetHost widgetHost)
    {
        var length = await Task.Run(() => widgetHost!.GetWidgets().Length);
        Log.Logger()?.ReportInfo("DashboardView", $"Found abandoned widget, try to delete it...");
        Log.Logger()?.ReportInfo("DashboardView", $"Before delete, {length} widgets for this host");

        await Task.Run(async () => await widget.DeleteAsync());

        var newWidgetList = await Task.Run(() => widgetHost.GetWidgets());
        length = (newWidgetList == null) ? 0 : newWidgetList.Length;
        Log.Logger()?.ReportInfo("DashboardView", $"After delete, {length} widgets for this host");
    }

    private async Task PlaceWidget(KeyValuePair<int, Widget> orderedWidget, int finalPlace)
    {
        var widget = orderedWidget.Value;
        var size = await Task.Run(async () => await widget.GetSizeAsync());
        await InsertWidgetInPinnedWidgetsAsync(widget, size, finalPlace);
        await WidgetHelpers.SetPositionCustomStateAsync(widget, finalPlace);
    }

    [RelayCommand]
    public async Task AddWidgetClickAsync()
    {
        // If this is the first time we're initializing the Dashboard, or if initialization failed last time, initialize now.
        if (!_widgetHostInitialized)
        {
            var initialized = await InitializeDashboard();
            if (!initialized)
            {
                var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");

                var errorDialog = new ContentDialog()
                {
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = this.ActualTheme,
                    Content = resourceLoader.GetString("UpdateWebExpContent"),
                    CloseButtonText = resourceLoader.GetString("UpdateWebExpCancel"),
                    PrimaryButtonText = resourceLoader.GetString("UpdateWebExpUpdate"),
                    PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
                };
                errorDialog.PrimaryButtonClick += async (ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
                {
                    await Launcher.LaunchUriAsync(new ("ms-windows-store://pdp/?productid=9MSSGKG348SP"));
                    sender.Hide();
                };
                _ = await errorDialog.ShowAsync();
                return;
            }
        }

        var dialog = new AddWidgetDialog(_dispatcher, ActualTheme)
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = this.XamlRoot,
            RequestedTheme = this.ActualTheme,
        };

        // If the dialog was closed in a way we don't already handle (for example, pressing Esc),
        // delete the partially created widget.
        dialog.Closed += async (sender, args) =>
        {
            if (dialog.AddedWidget == null && dialog.ViewModel != null && dialog.ViewModel.Widget != null)
            {
                await Task.Run(async () => await dialog.ViewModel.Widget.DeleteAsync());
            }
        };

        _ = await dialog.ShowAsync();

        var newWidget = dialog.AddedWidget;

        if (newWidget != null)
        {
            // Set custom state on new widget.
            var position = PinnedWidgets.Count;
            var newCustomState = WidgetHelpers.CreateWidgetCustomState(position);
            Log.Logger()?.ReportDebug("DashboardView", $"SetCustomState: {newCustomState}");
            await newWidget.SetCustomStateAsync(newCustomState);

            // Put new widget on the Dashboard.
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            var widgetDefinition = await Task.Run(() => widgetCatalog?.GetWidgetDefinition(newWidget.DefinitionId));
            if (widgetDefinition is not null)
            {
                var size = await Task.Run(() => WidgetHelpers.GetDefaultWidgetSize(widgetDefinition.GetWidgetCapabilities()));
                await Task.Run(async () => await newWidget.SetSizeAsync(size));
                await InsertWidgetInPinnedWidgetsAsync(newWidget, size, position);
            }
        }
    }

    private async Task InsertWidgetInPinnedWidgetsAsync(Widget widget, WidgetSize size, int index)
    {
        await Task.Run(async () =>
        {
            var widgetDefinitionId = widget.DefinitionId;
            var widgetId = widget.Id;
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            var widgetDefinition = await Task.Run(() => widgetCatalog?.GetWidgetDefinition(widgetDefinitionId));

            if (widgetDefinition != null)
            {
                Log.Logger()?.ReportInfo("DashboardView", $"Insert widget in pinned widgets, id = {widgetId}, index = {index}");

                TelemetryFactory.Get<ITelemetry>().Log(
                    "Dashboard_ReportPinnedWidget",
                    LogLevel.Critical,
                    new ReportPinnedWidgetEvent(widgetDefinition.ProviderDefinition.Id, widgetDefinitionId));

                var wvm = new WidgetViewModel(widget, size, widgetDefinition, _dispatcher);
                _dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        PinnedWidgets.Insert(index, wvm);
                    }
                    catch (Exception ex)
                    {
                        // TODO Support concurrency in dashboard. Today concurrent async execution can cause insertion errors.
                        // https://github.com/microsoft/devhome/issues/1215
                        Log.Logger()?.ReportWarn("DashboardView", $"Couldn't insert pinned widget", ex);
                    }
                });
            }
            else
            {
                // If the widget provider was uninstalled while we weren't running, the catalog won't have the definition so delete the widget.
                Log.Logger()?.ReportInfo("DashboardView", $"No widget definition '{widgetDefinitionId}', delete widget {widgetId} with that definition");
                try
                {
                    await widget.SetCustomStateAsync(string.Empty);
                    await widget.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger()?.ReportInfo("DashboardView", $"Error deleting widget", ex);
                }
            }
        });
    }

    private void WidgetCatalog_WidgetProviderDefinitionAdded(WidgetCatalog sender, WidgetProviderDefinitionAddedEventArgs args)
    {
        Log.Logger()?.ReportInfo("DashboardView", $"WidgetCatalog_WidgetProviderDefinitionAdded {args.ProviderDefinition.Id}");
    }

    private void WidgetCatalog_WidgetProviderDefinitionDeleted(WidgetCatalog sender, WidgetProviderDefinitionDeletedEventArgs args)
    {
        Log.Logger()?.ReportInfo("DashboardView", $"WidgetCatalog_WidgetProviderDefinitionDeleted {args.ProviderDefinitionId}");
    }

    private async void WidgetCatalog_WidgetDefinitionAdded(WidgetCatalog sender, WidgetDefinitionAddedEventArgs args)
    {
        var definition = await Task.Run(() => args.Definition);
        var definitionId = await Task.Run(() => definition.Id);
        Log.Logger()?.ReportInfo("DashboardView", $"WidgetCatalog_WidgetDefinitionAdded {definitionId}");
        await ViewModel.WidgetIconService.AddIconsToCacheAsync(definition);
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        var updatedDefinitionId = await Task.Run(() => args.Definition.Id);
        Log.Logger()?.ReportInfo("DashboardView", $"WidgetCatalog_WidgetDefinitionUpdated {updatedDefinitionId}");

        foreach (var widgetVMToUpdate in PinnedWidgets)
        {
            if (await Task.Run(() => widgetVMToUpdate.Widget.DefinitionId) != updatedDefinitionId)
            {
                continue;
            }

            // Things in the definition that we need to update to if they have changed:
            // AllowMultiple, DisplayTitle, Capabilities (size), ThemeResource (icons)
            var oldDef = widgetVMToUpdate.WidgetDefinition;
            var newDef = await Task.Run(() => args.Definition);

            // If we're no longer allowed to have multiple instances of this widget, delete all of them.
            if (await Task.Run(() => newDef.AllowMultiple) == false && await Task.Run(() => oldDef.AllowMultiple) == true)
            {
                _dispatcher.TryEnqueue(async () =>
                {
                    Log.Logger()?.ReportInfo("DashboardView", $"No longer allowed to have multiple of widget {newDef.Id}");
                    Log.Logger()?.ReportInfo("DashboardView", $"Delete widget {widgetVMToUpdate.Widget.Id}");
                    PinnedWidgets.Remove(widgetVMToUpdate);
                    await widgetVMToUpdate.Widget.DeleteAsync();
                    Log.Logger()?.ReportInfo("DashboardView", $"Deleted Widget {widgetVMToUpdate.Widget.Id}");
                });
            }
            else
            {
                // Changing the definition updates the DisplayTitle.
                widgetVMToUpdate.WidgetDefinition = newDef;

                // If the size the widget is currently set to is no longer supported by the widget, revert to its default size.
                // TODO: Need to update WidgetControl with now-valid sizes.
                // TODO: Properly compare widget capabilities.
                // https://github.com/microsoft/devhome/issues/641
                await Task.Run(async () =>
                {
                    if (oldDef.GetWidgetCapabilities() != newDef.GetWidgetCapabilities())
                    {
                        // TODO: handle the case where this change is made while Dev Home is not running -- how do we restore?
                        // https://github.com/microsoft/devhome/issues/641
                        if (!newDef.GetWidgetCapabilities().Any(cap => cap.Size == widgetVMToUpdate.WidgetSize))
                        {
                            var newDefaultSize = WidgetHelpers.GetDefaultWidgetSize(newDef.GetWidgetCapabilities());
                            widgetVMToUpdate.WidgetSize = newDefaultSize;
                            await widgetVMToUpdate.Widget.SetSizeAsync(newDefaultSize);
                        }
                    }
                });
            }

            // TODO: ThemeResource (icons) changed.
            // https://github.com/microsoft/devhome/issues/641
        }
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private async void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var definitionId = await Task.Run(() => args.DefinitionId);
        _dispatcher.TryEnqueue(async () =>
        {
            Log.Logger()?.ReportInfo("DashboardView", $"WidgetDefinitionDeleted {definitionId}");
            foreach (var widgetToRemove in PinnedWidgets.Where(x => x.Widget.DefinitionId == definitionId).ToList())
            {
                Log.Logger()?.ReportInfo("DashboardView", $"Remove widget {widgetToRemove.Widget.Id}");
                PinnedWidgets.Remove(widgetToRemove);

                // The widget definition is gone, so delete widgets with that definition.
                await widgetToRemove.Widget.DeleteAsync();
            }
        });

        ViewModel.WidgetIconService.RemoveIconsFromCache(definitionId);
    }

    private void WidgetGridView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        Log.Logger()?.ReportDebug("DashboardView", $"Drag starting");

        // When drag starts, save the WidgetViewModel and the original index of the widget being dragged.
        var draggedObject = e.Items.FirstOrDefault();
        var draggedWidgetViewModel = draggedObject as WidgetViewModel;
        e.Data.Properties.Add(DraggedWidget, draggedWidgetViewModel);
        e.Data.Properties.Add(DraggedIndex, PinnedWidgets.IndexOf(draggedWidgetViewModel));
    }

    private void WidgetControl_DragOver(object sender, DragEventArgs e)
    {
        // A widget may be dropped on top of another widget, in which case the dropped widget will take the target widget's place.
        if (e.Data != null)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
        else
        {
            // If the dragged item doesn't have a DataPackage, don't allow it to be dropped.
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }

    private async void WidgetControl_Drop(object sender, DragEventArgs e)
    {
        Log.Logger()?.ReportDebug("DashboardView", $"Drop starting");

        // If the the thing we're dragging isn't a widget, it might not have a DataPackage and we shouldn't do anything with it.
        if (e.Data == null)
        {
            return;
        }

        // When drop happens, get the original index of the widget that was dragged and dropped.
        var result = e.Data.Properties.TryGetValue(DraggedIndex, out var draggedIndexObject);
        if (!result || draggedIndexObject == null)
        {
            return;
        }

        var draggedIndex = (int)draggedIndexObject;

        // Get the index of the widget that was dropped onto -- the dragged widget will take the place of this one,
        // and this widget and all subsequent widgets will move over to the right.
        var droppedControl = sender as WidgetControl;
        var droppedIndex = WidgetGridView.Items.IndexOf(droppedControl.WidgetSource);
        Log.Logger()?.ReportInfo("DashboardView", $"Widget dragged from index {draggedIndex} to {droppedIndex}");

        // If the widget is dropped at the position it's already at, there's nothing to do.
        if (draggedIndex == droppedIndex)
        {
            return;
        }

        result = e.Data.Properties.TryGetValue(DraggedWidget, out var draggedObject);
        if (!result || draggedObject == null)
        {
            return;
        }

        var draggedWidgetViewModel = draggedObject as WidgetViewModel;

        // Remove the moved widget then insert it back in the collection at the new location. If the dropped widget was
        // moved from a lower index to a higher one, removing the moved widget before inserting it will ensure that any
        // widgets between the starting and ending indices move up to replace the removed widget. If the widget was
        // moved from a higher index to a lower one, then the order of removal and insertion doesn't matter.
        PinnedWidgets.RemoveAt(draggedIndex);
        var widgetPair = new KeyValuePair<int, Widget>(droppedIndex, draggedWidgetViewModel.Widget);
        await PlaceWidget(widgetPair, droppedIndex);

        // Update the CustomState Position of any widgets that were moved.
        // The widget that has been dropped has already been updated, so don't do it again here.
        var startIndex = draggedIndex < droppedIndex ? draggedIndex : droppedIndex + 1;
        var endIndex = draggedIndex < droppedIndex ? droppedIndex : draggedIndex + 1;
        for (var i = startIndex; i < endIndex; i++)
        {
            var widgetToUpdate = PinnedWidgets.ElementAt(i);
            await WidgetHelpers.SetPositionCustomStateAsync(widgetToUpdate.Widget, i);
        }

        Log.Logger()?.ReportDebug("DashboardView", $"Drop ended");
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
        AutomationProperties.SetName(resetButton, "ResetBannerButton");
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

        BannerViewModel.ResetDashboardBanner();
    }
#endif
}
