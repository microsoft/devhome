// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.Views;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Controls;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.TelemetryEvents;
using DevHome.Dashboard.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.UI.Core;

using VirtualKey = Windows.System.VirtualKey;

namespace DevHome.Dashboard.Views;

public partial class DashboardView : ToolPage, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DashboardView));

    public DashboardViewModel ViewModel { get; }

    internal DashboardBannerViewModel BannerViewModel { get; }

    private readonly WidgetViewModelFactory _widgetViewModelFactory;

    private readonly SemaphoreSlim _pinnedWidgetsLock = new(1, 1);
    private readonly SemaphoreSlim _moveWidgetsLock = new(1, 1);

    private static DispatcherQueue _dispatcherQueue;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWidgetExtensionService _widgetExtensionService;
    private readonly CancellationTokenSource _initWidgetsCancellationTokenSource = new();
    private bool _disposedValue;

    private const string DraggedWidget = "DraggedWidget";
    private const string DraggedIndex = "DraggedIndex";

    public DashboardView()
    {
        ViewModel = Application.Current.GetService<DashboardViewModel>();
        BannerViewModel = Application.Current.GetService<DashboardBannerViewModel>();
        _widgetViewModelFactory = Application.Current.GetService<WidgetViewModelFactory>();

        this.InitializeComponent();

        ViewModel.PinnedWidgets.CollectionChanged += OnPinnedWidgetsCollectionChangedAsync;

        _dispatcherQueue = Application.Current.GetService<DispatcherQueue>();
        _localSettingsService = Application.Current.GetService<ILocalSettingsService>();
        _widgetExtensionService = Application.Current.GetService<IWidgetExtensionService>();

#if DEBUG
        Loaded += AddResetButton;
#endif
    }

    private async Task<bool> SubscribeToWidgetCatalogEventsAsync()
    {
        _log.Information("SubscribeToWidgetCatalogEvents");

        try
        {
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                _log.Error("Error in in SubscribeToWidgetCatalogEvents, widgetCatalog == null");
                return false;
            }

            widgetCatalog!.WidgetDefinitionUpdated += WidgetCatalog_WidgetDefinitionUpdated;
            widgetCatalog!.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception in SubscribeToWidgetCatalogEvents:");
            return false;
        }

        return true;
    }

    private async Task UnsubscribeFromWidgetCatalogEventsAsync()
    {
        _log.Information("UnsubscribeFromWidgetCatalogEvents");

        try
        {
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                return;
            }

            widgetCatalog!.WidgetDefinitionUpdated -= WidgetCatalog_WidgetDefinitionUpdated;
            widgetCatalog!.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception in UnsubscribeFromWidgetCatalogEventsAsync:");
        }
    }

    private async void HandleRendererUpdated(object sender, object args)
    {
        // Re-render the widgets with the new theme and renderer.
        foreach (var widget in ViewModel.PinnedWidgets)
        {
            await widget.RenderAsync();
        }
    }

    [RelayCommand]
    private async Task OnLoadedAsync()
    {
        ViewModel.IsLoading = true;
        ViewModel.HasWidgetServiceInitialized = false;

        if (await ValidateDashboardState())
        {
            ViewModel.HasWidgetServiceInitialized = true;
            await InitializeDashboard();
        }

        TelemetryFactory.Get<ITelemetry>().Log(
            "Page_Loaded_Event",
            LogLevel.Critical,
            new PageLoadedEvent(GetType().Name));

        ViewModel.IsLoading = false;
    }

    [RelayCommand]
    private async Task OnUnloadedAsync()
    {
        _log.Debug($"Unloading Dashboard, cancel any loading.");
        _initWidgetsCancellationTokenSource?.Cancel();
        ViewModel.PinnedWidgets.CollectionChanged -= OnPinnedWidgetsCollectionChangedAsync;
        Bindings.StopTracking();

        Application.Current.GetService<WidgetAdaptiveCardRenderingService>().RendererUpdated -= HandleRendererUpdated;

        _log.Debug($"Leaving Dashboard, deactivating widgets.");

        await _pinnedWidgetsLock.WaitAsync();
        try
        {
            await Task.Run(() => UnsubscribeFromWidgets());
        }
        finally
        {
            ViewModel.PinnedWidgets.Clear();
            _pinnedWidgetsLock.Release();
        }

        await UnsubscribeFromWidgetCatalogEventsAsync();
    }

    private void UnsubscribeFromWidgets()
    {
        try
        {
            foreach (var widget in ViewModel.PinnedWidgets)
            {
                widget.UnsubscribeFromWidgetUpdates();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception in UnsubscribeFromWidgets:");
        }
    }

    private async Task<bool> ValidateDashboardState()
    {
        // Ensure we're not running elevated. Display an error and don't allow using the Dashboard if we are.
        if (ViewModel.IsRunningElevated())
        {
            _log.Error($"Dev Home is running as admin, can't show Dashboard");
            RunningAsAdminMessageStackPanel.Visibility = Visibility.Visible;
            return false;
        }

        // Ensure the WidgetService is installed and up to date.
        var widgetServiceState = ViewModel.WidgetServiceService.GetWidgetServiceState();
        switch (widgetServiceState)
        {
            case WidgetServiceService.WidgetServiceStates.MeetsMinVersion:
                _log.Information($"WidgetServiceState meets min version");
                break;
            case WidgetServiceService.WidgetServiceStates.NotAtMinVersion:
                _log.Warning($"Initialization failed, WidgetServiceState not at min version");
                UpdateWidgetsMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            case WidgetServiceService.WidgetServiceStates.NotOK:
                _log.Warning($"Initialization failed, WidgetServiceState not OK");
                NotOKServiceMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            case WidgetServiceService.WidgetServiceStates.Updating:
                _log.Warning($"Initialization failed, WidgetServiceState updating");
                UpdatingWidgetServiceMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            case WidgetServiceService.WidgetServiceStates.Unknown:
                _log.Error($"Initialization failed, WidgetServiceState unknown");
                RestartDevHomeMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            default:
                break;
        }

        // Ensure we can access the WidgetService and subscribe to WidgetCatalog events.
        if (!await SubscribeToWidgetCatalogEventsAsync())
        {
            _log.Error($"Catalog event subscriptions failed, show error");
            RestartDevHomeMessageStackPanel.Visibility = Visibility.Visible;
            return false;
        }

        return true;
    }

    private async Task InitializeDashboard()
    {
        var isFirstDashboardRun = !(await _localSettingsService.ReadSettingAsync<bool>(WellKnownSettingsKeys.IsNotFirstDashboardRun));
        _log.Information($"Is first dashboard run = {isFirstDashboardRun}");
        if (isFirstDashboardRun)
        {
            await _localSettingsService.SaveSettingAsync(WellKnownSettingsKeys.IsNotFirstDashboardRun, true);
        }

        try
        {
            await InitializePinnedWidgetListAsync(isFirstDashboardRun, _initWidgetsCancellationTokenSource.Token);
            Application.Current.GetService<WidgetAdaptiveCardRenderingService>().RendererUpdated += HandleRendererUpdated;
        }
        catch (OperationCanceledException ex)
        {
            _log.Information(ex, "InitializePinnedWidgetListAsync operation was cancelled.");
            return;
        }
    }

    private async Task InitializePinnedWidgetListAsync(bool isFirstDashboardRun, CancellationToken cancellationToken)
    {
        var hostWidgets = await GetPreviouslyPinnedWidgets();
        if ((hostWidgets.Length == 0) && isFirstDashboardRun)
        {
            // If it's the first time the Dashboard has been displayed and we have no other widgets pinned to a
            // different version of Dev Home, pin some default widgets.
            _log.Information($"Pin default widgets");
            await _pinnedWidgetsLock.WaitAsync(CancellationToken.None);
            try
            {
                await PinDefaultWidgetsAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                // If the operation is cancelled, delete any default widgets that were already pinned and reset the IsNotFirstDashboardRun setting.
                // Next time the user opens the Dashboard, treat it as the first run again.
                _log.Information(ex, "PinDefaultWidgetsAsync operation was cancelled, delete any widgets already pinned");
                foreach (var widget in ViewModel.PinnedWidgets)
                {
                    await widget.Widget.DeleteAsync();
                }

                await _localSettingsService.SaveSettingAsync(WellKnownSettingsKeys.IsNotFirstDashboardRun, false);
            }
            finally
            {
                _pinnedWidgetsLock.Release();
            }
        }
        else
        {
            await _pinnedWidgetsLock.WaitAsync(CancellationToken.None);
            try
            {
                await RestorePinnedWidgetsAsync(hostWidgets, cancellationToken);
            }
            finally
            {
                // No cleanup to do if the operation is cancelled.
                _pinnedWidgetsLock.Release();
            }
        }
    }

    private async Task<ComSafeWidget[]> GetPreviouslyPinnedWidgets()
    {
        _log.Information("Get widgets for current host");
        var unsafeHostWidgets = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        if (unsafeHostWidgets.Length == 0)
        {
            _log.Information($"Found 0 widgets for this host");
            return [];
        }

        var comSafeHostWidgets = new List<ComSafeWidget>();
        foreach (var unsafeWidget in unsafeHostWidgets)
        {
            var id = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (!string.IsNullOrEmpty(id))
            {
                var comSafeWidget = new ComSafeWidget(id);
                if (await comSafeWidget.PopulateAsync())
                {
                    comSafeHostWidgets.Add(comSafeWidget);
                }
            }
        }

        _log.Information($"Found {comSafeHostWidgets.Count} widgets for this host");

        return [.. comSafeHostWidgets];
    }

    private async Task RestorePinnedWidgetsAsync(ComSafeWidget[] hostWidgets, CancellationToken cancellationToken)
    {
        var restoredWidgetsWithPosition = new SortedDictionary<int, ComSafeWidget>();
        var restoredWidgetsWithoutPosition = new SortedDictionary<int, ComSafeWidget>();
        var numUnorderedWidgets = 0;

        var pinnedSingleInstanceWidgets = new List<string>();

        _log.Information($"Restore pinned widgets");

        // Widgets do not come from the host in a deterministic order, so save their order in each widget's CustomState.
        // Iterate through all the widgets and put them in order. If a widget does not have a position assigned to it,
        // append it at the end. If a position is missing, just show the next widget in order.
        foreach (var widget in hostWidgets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var stateStr = await widget.GetCustomStateAsync();
                _log.Information($"GetWidgetCustomState: {stateStr}");

                if (string.IsNullOrEmpty(stateStr))
                {
                    // If we have a widget with no state, Dev Home does not consider it a valid widget
                    // and should delete it, rather than letting it run invisibly in the background.
                    await DeleteAbandonedWidgetAsync(widget);
                    continue;
                }

                var stateObj = System.Text.Json.JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);
                if (stateObj.Host != WidgetHelpers.DevHomeHostName)
                {
                    // This shouldn't be able to be reached
                    _log.Error($"Widget has custom state but no HostName.");
                    continue;
                }

                var widgetDefinitionId = widget.DefinitionId;
                var unsafeWidgetDefinition = await ViewModel.WidgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
                if (unsafeWidgetDefinition == null)
                {
                    await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
                    continue;
                }

                var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
                if (!await comSafeWidgetDefinition.PopulateAsync())
                {
                    _log.Error($"Error populating widget definition for widget {widgetDefinitionId}");
                    await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
                    continue;
                }

                // If the widget's extension was disabled, hide the widget (don't add it to the list), but don't delete it.
                if (!await WidgetHelpers.IsIncludedWidgetProviderAsync(comSafeWidgetDefinition.ProviderDefinition))
                {
                    _log.Information($"Not adding widget from disabled extension {comSafeWidgetDefinition.ProviderDefinitionId}");
                    continue;
                }

                // Ensure only one copy of a widget is pinned if that widget's definition only allows for one instance.
                if (comSafeWidgetDefinition.AllowMultiple == false)
                {
                    if (pinnedSingleInstanceWidgets.Contains(widgetDefinitionId))
                    {
                        _log.Information($"No longer allowed to have multiple of widget {widgetDefinitionId}");
                        await widget.DeleteAsync();
                        _log.Information($"Deleted Widget {widgetDefinitionId} and not adding it to PinnedWidgets");
                        continue;
                    }
                    else
                    {
                        pinnedSingleInstanceWidgets.Add(widgetDefinitionId);
                    }
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
                _log.Error(ex, $"RestorePinnedWidgets(): ");
            }
        }

        // Merge the dictionaries for easier looping. restoredWidgetsWithoutPosition should be empty, so this should be fast.
        var lastOrderedKey = restoredWidgetsWithPosition.Count > 0 ? restoredWidgetsWithPosition.Last().Key : -1;
        restoredWidgetsWithoutPosition.ToList().ForEach(x => restoredWidgetsWithPosition.Add(++lastOrderedKey, x.Value));

        // Now that we've ordered the widgets, put them in their final collection.
        var finalPlace = 0;
        foreach (var orderedWidget in restoredWidgetsWithPosition)
        {
            var comSafeWidget = orderedWidget.Value;
            var size = await comSafeWidget.GetSizeAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, finalPlace++, cancellationToken);
        }

        // Go through the newly created list of pinned widgets and update any positions that may have changed.
        // For example, if the provider for the widget at position 0 was deleted, the widget at position 1
        // should be updated to have position 0, etc.
        var updatedPlace = 0;
        foreach (var widget in ViewModel.PinnedWidgets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await WidgetHelpers.SetPositionCustomStateAsync(widget.Widget, updatedPlace++);
        }

        _log.Information($"Done restoring pinned widgets");
    }

    private async Task DeleteAbandonedWidgetAsync(ComSafeWidget widget)
    {
        var widgetList = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        var length = widgetList.Length;
        _log.Information($"Found abandoned widget, try to delete it...");
        _log.Information($"Before delete, {length} widgets for this host");

        await widget.DeleteAsync();

        var newWidgetList = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        length = newWidgetList.Length;
        _log.Information($"After delete, {length} widgets for this host");
    }

    private async Task PinDefaultWidgetsAsync(CancellationToken cancellationToken)
    {
        var comSafeWidgetDefinitions = await ComSafeHelpers.GetAllOrderedComSafeWidgetDefinitions(ViewModel.WidgetHostingService);
        foreach (var comSafeWidgetDefinition in comSafeWidgetDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = comSafeWidgetDefinition.Id;
            if (WidgetHelpers.DefaultWidgetDefinitionIds.Contains(id))
            {
                _log.Information($"Found default widget {id}");
                await PinDefaultWidgetAsync(comSafeWidgetDefinition, cancellationToken);
            }
        }
    }

    private async Task PinDefaultWidgetAsync(ComSafeWidgetDefinition defaultWidgetDefinition, CancellationToken cancellationToken)
    {
        try
        {
            // Create widget
            var size = WidgetHelpers.GetDefaultWidgetSize(await defaultWidgetDefinition.GetWidgetCapabilitiesAsync());
            var definitionId = defaultWidgetDefinition.Id;
            var unsafeWidget = await ViewModel.WidgetHostingService.CreateWidgetAsync(definitionId, size);
            if (unsafeWidget == null)
            {
                // Fail silently, since this is only the default widget and not a response to user action.
                return;
            }

            var unsafeWidgetId = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (unsafeWidgetId == string.Empty)
            {
                // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                // We can fail silently since this isn't in response to user action.
                _log.Error("Couldn't get Widget.Id, can't create the widget");
                await unsafeWidget.DeleteAsync();
                return;
            }

            var comSafeWidget = new ComSafeWidget(unsafeWidgetId);
            if (!await comSafeWidget.PopulateAsync())
            {
                // If we created the widget but can't populate the ComSafeWidget, delete the widget.
                // We can fail silently since this isn't in response to user action.
                _log.Error("Couldn't populate ComSafeWidget, can't create the widget");
                await unsafeWidget.DeleteAsync();
                return;
            }

            _log.Information($"Created default widget {unsafeWidgetId}");

            // Set custom state on new widget.
            var position = ViewModel.PinnedWidgets.Count;
            var newCustomState = WidgetHelpers.CreateWidgetCustomState(position);
            _log.Debug($"SetCustomState: {newCustomState}");
            await comSafeWidget.SetCustomStateAsync(newCustomState);

            // Put new widget on the Dashboard.
            await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, position, cancellationToken);
            _log.Information($"Inserted default widget {unsafeWidgetId} at position {position}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // We can fail silently since this isn't in response to user action.
            _log.Error(ex, $"PinDefaultWidget failed: ");
        }
    }

    [RelayCommand]
    public async Task GoToWidgetsInStoreAsync()
    {
        if (RuntimeHelper.IsOnWindows11)
        {
            await Windows.System.Launcher.LaunchUriAsync(new($"ms-windows-store://pdp/?productid={WidgetHelpers.WebExperiencePackPackageId}"));
        }
        else
        {
            await Windows.System.Launcher.LaunchUriAsync(new($"ms-windows-store://pdp/?productid={WidgetHelpers.WidgetsPlatformRuntimePackageId}"));
        }
    }

    [RelayCommand]
    public async Task AddWidgetClickAsync()
    {
        var dialog = new AddWidgetDialog()
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = this.XamlRoot,
        };

        _ = await dialog.ShowAsync();

        var newWidgetDefinition = dialog.AddedWidget;

        if (newWidgetDefinition != null)
        {
            try
            {
                var size = WidgetHelpers.GetDefaultWidgetSize(await newWidgetDefinition.GetWidgetCapabilitiesAsync());
                var unsafeWidget = await ViewModel.WidgetHostingService.CreateWidgetAsync(newWidgetDefinition.Id, size);
                if (unsafeWidget == null)
                {
                    // Couldn't create the widget, show an error message.
                    _log.Error($"Failure in CreateWidgetAsync, can't create the widget");
                    await ShowCreateWidgetErrorMessage();
                    return;
                }

                var unsafeWidgetId = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
                if (unsafeWidgetId == string.Empty)
                {
                    _log.Error($"Couldn't get Widget.Id, can't create the widget");
                    await ShowCreateWidgetErrorMessage();

                    // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                    // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                    await TryDeleteUnsafeWidget(unsafeWidget);
                    return;
                }

                var comSafeWidget = new ComSafeWidget(unsafeWidgetId);
                if (!await comSafeWidget.PopulateAsync())
                {
                    _log.Error($"Couldn't populate the ComSafeWidget, can't create the widget");
                    await ShowCreateWidgetErrorMessage();

                    // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                    // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                    await TryDeleteUnsafeWidget(unsafeWidget);
                    return;
                }

                // Set custom state on new widget.
                var position = ViewModel.PinnedWidgets.Count;
                var newCustomState = WidgetHelpers.CreateWidgetCustomState(position);
                _log.Debug($"SetCustomState: {newCustomState}");
                await comSafeWidget.SetCustomStateAsync(newCustomState);

                // Put new widget on the Dashboard.
                await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, position);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, $"Creating widget failed: ");
                await ShowCreateWidgetErrorMessage();
            }
        }
    }

    private async Task TryDeleteUnsafeWidget(Widget unsafeWidget)
    {
        try
        {
            await unsafeWidget.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error deleting widget");
        }
    }

    private async Task ShowCreateWidgetErrorMessage()
    {
        var mainWindow = Application.Current.GetService<Window>();
        var stringResource = new StringResource("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
        await mainWindow.ShowErrorMessageDialogAsync(
            title: string.Empty,
            content: stringResource.GetLocalized("CouldNotCreateWidgetError"),
            buttonText: stringResource.GetLocalized("CloseButtonText"));
    }

    private async Task InsertWidgetInPinnedWidgetsAsync(ComSafeWidget widget, WidgetSize size, int index, CancellationToken cancellationToken = default)
    {
        await Task.Run(
            async () =>
        {
            var widgetDefinitionId = widget.DefinitionId;
            var widgetId = widget.Id;
            _log.Information($"Insert widget in pinned widgets, id = {widgetId}, index = {index}");

            var unsafeWidgetDefinition = await ViewModel.WidgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
            if (unsafeWidgetDefinition != null)
            {
                var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
                if (!await comSafeWidgetDefinition.PopulateAsync())
                {
                    _log.Error($"Error inserting widget in pinned widgets, id = {widgetId}, index = {index}");
                    await widget.DeleteAsync();
                    return;
                }

                // The WidgetService will start the widget provider, however Dev Home won't know about it and won't be
                // able to send disposed events when Dev Home closes. Ensure the provider is started here so we can
                // tell the extension to dispose later.
                if (_widgetExtensionService.IsCoreWidgetProvider(comSafeWidgetDefinition.ProviderDefinitionId))
                {
                    await _widgetExtensionService.EnsureCoreWidgetExtensionStarted(comSafeWidgetDefinition.ProviderDefinitionId);
                }

                TelemetryFactory.Get<ITelemetry>().Log(
                    "Dashboard_ReportPinnedWidget",
                    LogLevel.Critical,
                    new ReportPinnedWidgetEvent(comSafeWidgetDefinition.ProviderDefinitionId, widgetDefinitionId));

                var wvm = _widgetViewModelFactory(widget, size, comSafeWidgetDefinition);
                cancellationToken.ThrowIfCancellationRequested();

                _dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        ViewModel.PinnedWidgets.Insert(index, wvm);
                    }
                    catch (Exception ex)
                    {
                        // TODO Support concurrency in dashboard. Today concurrent async execution can cause insertion errors.
                        // https://github.com/microsoft/devhome/issues/1215
                        _log.Warning(ex, $"Couldn't insert pinned widget");
                    }
                });
            }
            else
            {
                await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
            }
        },
            cancellationToken);
    }

    private async Task DeleteWidgetWithNoDefinition(ComSafeWidget widget, string widgetDefinitionId)
    {
        // If the widget provider was uninstalled while we weren't running, the catalog won't have the definition so delete the widget.
        _log.Information($"No widget definition '{widgetDefinitionId}', delete widget with that definition");
        try
        {
            await widget.SetCustomStateAsync(string.Empty);
            await widget.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.Information(ex, $"Error deleting widget");
        }
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        WidgetDefinition unsafeWidgetDefinition;
        try
        {
            unsafeWidgetDefinition = await Task.Run(() => args.Definition);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "WidgetCatalog_WidgetDefinitionUpdated: Couldn't get args.WidgetDefinition");
            return;
        }

        if (unsafeWidgetDefinition == null)
        {
            _log.Error("WidgetCatalog_WidgetDefinitionUpdated: Couldn't get WidgetDefinition");
            return;
        }

        var widgetDefinitionId = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
        var comSafeNewDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
        if (!await comSafeNewDefinition.PopulateAsync())
        {
            _log.Error($"Error populating widget definition for widget {widgetDefinitionId}");
            return;
        }

        var updatedDefinitionId = comSafeNewDefinition.Id;
        _log.Information($"WidgetCatalog_WidgetDefinitionUpdated {updatedDefinitionId}");

        var matchingWidgetsFound = 0;

        foreach (var widgetToUpdate in ViewModel.PinnedWidgets.Where(x => x.Widget.DefinitionId == updatedDefinitionId).ToList())
        {
            // Things in the definition that we need to update to if they have changed:
            // AllowMultiple, DisplayTitle, Capabilities (size), ThemeResource (icons)
            var oldDef = widgetToUpdate.WidgetDefinition;

            // If we're no longer allowed to have multiple instances of this widget, delete all but the first.
            if (++matchingWidgetsFound > 1 && comSafeNewDefinition.AllowMultiple == false && oldDef.AllowMultiple == true)
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    _log.Information($"No longer allowed to have multiple of widget {updatedDefinitionId}");
                    _log.Information($"Delete widget {widgetToUpdate.Widget.Id}");
                    ViewModel.PinnedWidgets.Remove(widgetToUpdate);
                    await widgetToUpdate.Widget.DeleteAsync();
                    _log.Information($"Deleted Widget {widgetToUpdate.Widget.Id}");
                });
            }
            else
            {
                // Changing the definition updates the DisplayTitle.
                widgetToUpdate.WidgetDefinition = comSafeNewDefinition;

                // If the size the widget is currently set to is no longer supported by the widget, revert to its default size.
                // TODO: Need to update WidgetControl with now-valid sizes.
                // TODO: Properly compare widget capabilities.
                // https://github.com/microsoft/devhome/issues/641
                if (await oldDef.GetWidgetCapabilitiesAsync() != await comSafeNewDefinition.GetWidgetCapabilitiesAsync())
                {
                    // TODO: handle the case where this change is made while Dev Home is not running -- how do we restore?
                    // https://github.com/microsoft/devhome/issues/641
                    if (!(await comSafeNewDefinition.GetWidgetCapabilitiesAsync()).Any(cap => cap.Size == widgetToUpdate.WidgetSize))
                    {
                        var newDefaultSize = WidgetHelpers.GetDefaultWidgetSize(await comSafeNewDefinition.GetWidgetCapabilitiesAsync());
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
        _dispatcherQueue.TryEnqueue(async () =>
        {
            _log.Information($"WidgetDefinitionDeleted {definitionId}");
            foreach (var widgetToRemove in ViewModel.PinnedWidgets.Where(x => x.Widget.DefinitionId == definitionId).ToList())
            {
                _log.Information($"Remove widget {widgetToRemove.Widget.Id}");
                ViewModel.PinnedWidgets.Remove(widgetToRemove);

                // The widget definition is gone, so delete widgets with that definition.
                await widgetToRemove.Widget.DeleteAsync();
            }
        });

        ViewModel.WidgetIconService.RemoveIconsFromCache(definitionId);
        ViewModel.WidgetScreenshotService.RemoveScreenshotsFromCache(definitionId);
    }

    // If a widget is removed from the list, update the saved positions of the following widgets.
    // If not updated, widgets pinned later may be assigned the same position as existing widgets,
    // since the saved position may be greater than the number of pinned widgets.
    // Unsubscribe from this event during drag and drop, since the drop event takes care of re-numbering.
    private async void OnPinnedWidgetsCollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            await _pinnedWidgetsLock.WaitAsync();
            try
            {
                var removedIndex = e.OldStartingIndex;
                _log.Debug($"Removed widget at index {removedIndex}");
                for (var i = removedIndex; i < ViewModel.PinnedWidgets.Count; i++)
                {
                    _log.Debug($"Updating widget position for widget now at {i}");
                    var widgetToUpdate = ViewModel.PinnedWidgets.ElementAt(i);
                    await WidgetHelpers.SetPositionCustomStateAsync(widgetToUpdate.Widget, i);
                }
            }
            finally
            {
                _pinnedWidgetsLock.Release();
            }
        }
    }

    private void WidgetGridView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        _log.Debug($"Drag starting");

        // When drag starts, save the WidgetViewModel and the original index of the widget being dragged.
        var draggedObject = e.Items.FirstOrDefault();
        var draggedWidgetViewModel = draggedObject as WidgetViewModel;
        e.Data.Properties.Add(DraggedWidget, draggedWidgetViewModel);
        e.Data.Properties.Add(DraggedIndex, ViewModel.PinnedWidgets.IndexOf(draggedWidgetViewModel));
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
        _log.Debug($"Drop starting");

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
        _log.Information($"Widget dragged from index {draggedIndex} to {droppedIndex}");

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

        await MoveWidgetAsync(draggedWidgetViewModel, draggedIndex, droppedIndex);

        _log.Debug($"Drop ended");
    }

    private async Task MoveWidgetAsync(WidgetViewModel draggedWidgetViewModel, int draggedIndex, int droppedIndex)
    {
        // Remove the moved widget then insert it back in the collection at the new location. If the dropped widget was
        // moved from a lower index to a higher one, removing the moved widget before inserting it will ensure that any
        // widgets between the starting and ending indices move up to replace the removed widget. If the widget was
        // moved from a higher index to a lower one, then the order of removal and insertion doesn't matter.
        ViewModel.PinnedWidgets.CollectionChanged -= OnPinnedWidgetsCollectionChangedAsync;

        var size = await draggedWidgetViewModel.Widget.GetSizeAsync();

        // Doing these operations in different orders for different start/end indexes make the animation look a little better.
        if (draggedIndex < droppedIndex)
        {
            ViewModel.PinnedWidgets.RemoveAt(draggedIndex);
            await InsertWidgetInPinnedWidgetsAsync(draggedWidgetViewModel.Widget, size, droppedIndex);
        }
        else
        {
            await InsertWidgetInPinnedWidgetsAsync(draggedWidgetViewModel.Widget, size, droppedIndex);
            ViewModel.PinnedWidgets.RemoveAt(draggedIndex + 1);
        }

        await WidgetHelpers.SetPositionCustomStateAsync(draggedWidgetViewModel.Widget, droppedIndex);

        // Update the CustomState Position of any widgets that were moved.
        // The widget that has been dropped has already been updated, so don't do it again here.
        var startIndex = draggedIndex < droppedIndex ? draggedIndex : droppedIndex + 1;
        var endIndex = draggedIndex < droppedIndex ? droppedIndex : draggedIndex + 1;
        for (var i = startIndex; i < endIndex; i++)
        {
            var widgetToUpdate = ViewModel.PinnedWidgets.ElementAt(i);
            await WidgetHelpers.SetPositionCustomStateAsync(widgetToUpdate.Widget, i);
        }

        ViewModel.PinnedWidgets.CollectionChanged += OnPinnedWidgetsCollectionChangedAsync;
    }

    /// <summary>
    /// Handle keyboard shortcuts for moving widgets left and right.
    /// </summary>
    private async void HandleKeyUpAsync(object sender, KeyRoutedEventArgs e)
    {
        _log.Debug($"Key up");

        await _moveWidgetsLock.WaitAsync();
        try
        {
            var key = e.Key;
            _log.Debug($"e.Key = {key}");
            if (e.Handled || !(key == VirtualKey.Left || key == VirtualKey.Right))
            {
                return;
            }

            var focusedItem = e.OriginalSource as GridViewItem;
            if (focusedItem?.Content is not WidgetViewModel widgetViewModel)
            {
                return;
            }

            if (IsAltAndShiftPressed())
            {
                var index = ViewModel.PinnedWidgets.IndexOf(widgetViewModel);
                _log.Information($"Move widget {widgetViewModel.WidgetDisplayTitle} at index {index} {key}");

                if (key == VirtualKey.Left && index > 0)
                {
                    await MoveWidgetAsync(widgetViewModel, index, index - 1);
                    await FocusManager.TryFocusAsync(WidgetGridView.ItemsPanelRoot.Children.ElementAt(index - 1), FocusState.Keyboard);
                    _log.Debug($"Focus moved to index {index - 1}");
                }
                else if (key == VirtualKey.Right && index < (ViewModel.PinnedWidgets.Count - 1))
                {
                    // Setting focus before and after the move looks more natural than letting the focus move to the wrong element.
                    await FocusManager.TryFocusAsync(WidgetGridView.ItemsPanelRoot.Children.ElementAt(index + 1), FocusState.Keyboard);
                    await MoveWidgetAsync(widgetViewModel, index, index + 1);
                    await FocusManager.TryFocusAsync(WidgetGridView.ItemsPanelRoot.Children.ElementAt(index + 1), FocusState.Keyboard);
                    _log.Debug($"Focus moved to index {index + 1}");
                }
            }

            e.Handled = true;
        }
        finally
        {
            _moveWidgetsLock.Release();
        }
    }

    private bool IsAltAndShiftPressed()
    {
        var downState = CoreVirtualKeyStates.Down;

        // VirtualKeys "Menu" key is also the "Alt" key on the keyboard.
        var isAltKeyPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & downState) == downState;
        var isShiftKeyPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & downState) == downState;
        _log.Debug($"isAltKeyPressed = {isAltKeyPressed} isShiftKeyPressed = {isShiftKeyPressed}");

        return isAltKeyPressed && isShiftKeyPressed;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _pinnedWidgetsLock.Dispose();
                _moveWidgetsLock.Dispose();
            }

            _disposedValue = true;
        }
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

#pragma warning disable CA1853
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
