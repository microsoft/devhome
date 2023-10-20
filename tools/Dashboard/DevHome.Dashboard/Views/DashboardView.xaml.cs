// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Controls;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
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

    public DashboardViewModel ViewModel { get; }

    public static ObservableCollection<WidgetViewModel> PinnedWidgets { get; set; }

    private static Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private static bool _widgetHostInitialized;

    private const string DraggedWidget = "DraggedWidget";
    private const string DraggedIndex = "DraggedIndex";

    public DashboardView()
    {
        ViewModel = Application.Current.GetService<DashboardViewModel>();

        this.InitializeComponent();

        if (PinnedWidgets != null)
        {
            PinnedWidgets.CollectionChanged -= OnPinnedWidgetsCollectionChanged;
        }

        PinnedWidgets = new ObservableCollection<WidgetViewModel>();
        PinnedWidgets.CollectionChanged += OnPinnedWidgetsCollectionChanged;

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        ActualThemeChanged += OnActualThemeChanged;

        Loaded += OnLoaded;

#if DEBUG
        Loaded += AddResetButton;
#endif
    }

    private bool SubscribeToWidgetCatalogEvents()
    {
        Log.Logger()?.ReportInfo("DashboardView", "SubscribeToWidgetCatalogEvents");

        try
        {
            ViewModel.WidgetHostingService.GetWidgetCatalog()!.WidgetProviderDefinitionAdded += WidgetCatalog_WidgetProviderDefinitionAdded;
            ViewModel.WidgetHostingService.GetWidgetCatalog()!.WidgetProviderDefinitionDeleted += WidgetCatalog_WidgetProviderDefinitionDeleted;
            ViewModel.WidgetHostingService.GetWidgetCatalog()!.WidgetDefinitionAdded += WidgetCatalog_WidgetDefinitionAdded;
            ViewModel.WidgetHostingService.GetWidgetCatalog()!.WidgetDefinitionUpdated += WidgetCatalog_WidgetDefinitionUpdated;
            ViewModel.WidgetHostingService.GetWidgetCatalog()!.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
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
        foreach (var widget in PinnedWidgets)
        {
            await widget.RenderAsync();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeDashboard();
    }

    private bool EnsureHostingInitialized()
    {
        if (_widgetHostInitialized)
        {
            return _widgetHostInitialized;
        }

        _widgetHostInitialized = ViewModel.EnsureWebExperiencePack() && ViewModel.WidgetHostingService.GetWidgetCatalog() != null && SubscribeToWidgetCatalogEvents();

        return _widgetHostInitialized;
    }

    private async Task<bool> InitializeDashboard()
    {
        LoadingWidgetsProgressRing.Visibility = Visibility.Visible;
        ViewModel.IsLoading = true;

        if (EnsureHostingInitialized())
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
        var pinnedWidgets = ViewModel.WidgetHostingService.GetWidgetHost()?.GetWidgets();
        if (pinnedWidgets != null)
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Found {pinnedWidgets.Length} widgets for this host");
            var restoredWidgetsWithPosition = new SortedDictionary<int, Widget>();
            var restoredWidgetsWithoutPosition = new SortedDictionary<int, Widget>();
            var numUnorderedWidgets = 0;

            // Widgets do not come from the host in a deterministic order, so save their order in each widget's CustomState.
            // Iterate through all the widgets and put them in order. If a widget does not have a position assigned to it,
            // append it at the end. If a position is missing, just show the next widget in order.
            foreach (var widget in pinnedWidgets)
            {
                try
                {
                    var stateStr = await widget.GetCustomStateAsync();
                    Log.Logger()?.ReportInfo("DashboardView", $"GetWidgetCustomState: {stateStr}");
                    if (!string.IsNullOrEmpty(stateStr))
                    {
                        var stateObj = System.Text.Json.JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);

                        if (stateObj.Host == WidgetHelpers.DevHomeHostName)
                        {
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
        else
        {
            Log.Logger()?.ReportInfo("DashboardView", $"Found 0 widgets for this host");
        }
    }

    private async Task PlaceWidget(KeyValuePair<int, Widget> orderedWidget, int finalPlace)
    {
        var widget = orderedWidget.Value;
        var size = await widget.GetSizeAsync();
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
            var widgetDef = ViewModel.WidgetHostingService.GetWidgetCatalog()!.GetWidgetDefinition(newWidget.DefinitionId);
            if (widgetDef is not null)
            {
                var size = WidgetHelpers.GetDefaultWidgetSize(widgetDef.GetWidgetCapabilities());
                await newWidget.SetSizeAsync(size);
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
            var widgetDefinition = ViewModel.WidgetHostingService.GetWidgetCatalog()!.GetWidgetDefinition(widgetDefinitionId);

            if (widgetDefinition != null)
            {
                Log.Logger()?.ReportInfo("DashboardView", $"Insert widget in pinned widgets, id = {widgetId}, index = {index}");
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
        Log.Logger()?.ReportInfo("DashboardView", $"WidgetCatalog_WidgetDefinitionAdded {args.Definition.Id}");
        await ViewModel.WidgetIconService.AddIconsToCacheAsync(args.Definition);
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        var updatedDefinitionId = args.Definition.Id;
        Log.Logger()?.ReportInfo("DashboardView", $"WidgetCatalog_WidgetDefinitionUpdated {updatedDefinitionId}");

        foreach (var widgetToUpdate in PinnedWidgets.Where(x => x.Widget.DefinitionId == updatedDefinitionId).ToList())
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
                // https://github.com/microsoft/devhome/issues/641
                if (oldDef.GetWidgetCapabilities() != newDef.GetWidgetCapabilities())
                {
                    // TODO: handle the case where this change is made while Dev Home is not running -- how do we restore?
                    // https://github.com/microsoft/devhome/issues/641
                    if (!newDef.GetWidgetCapabilities().Any(cap => cap.Size == widgetToUpdate.WidgetSize))
                    {
                        var newDefaultSize = WidgetHelpers.GetDefaultWidgetSize(newDef.GetWidgetCapabilities());
                        widgetToUpdate.WidgetSize = newDefaultSize;
                        await widgetToUpdate.Widget.SetSizeAsync(newDefaultSize);
                    }
                }
            }

            // TODO: ThemeResource (icons) changed.
            // https://github.com/microsoft/devhome/issues/641
        }
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var definitionId = args.DefinitionId;
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
        var widgetDef = ViewModel.WidgetHostingService.GetWidgetCatalog()!.GetWidgetDefinition(widgetViewModel.Widget.DefinitionId);

        var dialog = new CustomizeWidgetDialog(_dispatcher, widgetDef)
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
            await InsertWidgetInPinnedWidgetsAsync(newWidget, originalSize, index);
        }

        widgetViewModel.IsInEditMode = false;
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

        ViewModel.ResetDashboardBanner();
    }
#endif
}
