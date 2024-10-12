// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Services;
using DevHome.Common.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// The main view model for the landing page of the Environments tool.
/// </summary>
public partial class LandingPageViewModel : ObservableObject, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(LandingPageViewModel));

    private readonly AutoResetEvent _computeSystemLoadWait = new(false);

    private readonly Window _mainWindow;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly INavigationService _navigationService;

    private readonly IExtensionService _extensionService;

    private readonly StringResource _stringResource;

    private readonly object _lock = new();

    private EnvironmentsNotificationHelper? _notificationsHelper;

    private bool _disposed;

    private bool _wasSyncButtonClicked;

    private bool _extensionsToggled;

    private string _selectedProvider = string.Empty;

    public bool IsLoading { get; set; }

    public ObservableCollection<ComputeSystemCardBase> ComputeSystemCards { get; set; } = new();

    public AdvancedCollectionView ComputeSystemCardsView { get; set; }

    [ObservableProperty]
    private ExtensionInstallationViewModel _installationViewModel;

    public bool HasPageLoadedForTheFirstTime { get; set; }

    [ObservableProperty]
    private bool _shouldNavigateToExtensionsPage;

    [ObservableProperty]
    private string? _callToActionText;

    [ObservableProperty]
    private string? _callToActionHyperLinkButtonText;

    [ObservableProperty]
    private bool _showLoadingShimmer = true;

    [ObservableProperty]
    private int _selectedProviderIndex;

    [ObservableProperty]
    private int _selectedSortIndex;

    [ObservableProperty]
    private string _lastSyncTime;

    [ObservableProperty]
    private bool _shouldShowCreationHeader;

    private enum SortOptions
    {
        Alphabetical,
        AlphabeticalDescending,
        LastConnected,
    }

    private const int DefaultSortIndex = (int)SortOptions.LastConnected;

    public ObservableCollection<string> Providers { get; set; }

    private CancellationTokenSource _cancellationTokenSource = new();

    public LandingPageViewModel(
        INavigationService navigationService,
        IComputeSystemManager manager,
        ExtensionInstallationViewModel installationViewModel,
        IExtensionService extensionService,
        Window mainWindow)
    {
        _computeSystemManager = manager;
        _mainWindow = mainWindow;
        _navigationService = navigationService;

        _stringResource = new StringResource("DevHome.Environments.pri", "DevHome.Environments/Resources");

        SelectedSortIndex = DefaultSortIndex;
        Providers = new() { _stringResource.GetLocalized("AllProviders") };
        _lastSyncTime = _stringResource.GetLocalized("MomentsAgo");
        ComputeSystemCardsView = new AdvancedCollectionView(ComputeSystemCards);
        ComputeSystemCardsView.SortDescriptions.Add(new SortDescription("IsCardCreating", SortDirection.Descending));
        extensionService.ExtensionToggled += OnExtensionToggled;
        _extensionService = extensionService;
        _installationViewModel = installationViewModel;
        _installationViewModel.ExtensionChangedEvent += OnExtensionsChanged;
    }

    private void OnExtensionToggled(IExtensionService sender, IExtensionWrapper extension)
    {
        if (extension.HasProviderType(ProviderType.ComputeSystem))
        {
            _extensionsToggled = true;
        }
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = new(notificationQueue);
    }

    public void OnExtensionsChanged(object? sender, EventArgs args)
    {
        _mainWindow.DispatcherQueue.TryEnqueue(async () =>
        {
            await SyncButton();
        });
    }

    [RelayCommand]
    private async Task OnLoadedAsync()
    {
        await InstallationViewModel.UpdateExtensionPackageInfoAsync();
        await LoadModelAsync();
    }

    [RelayCommand]
    public async Task SyncButton()
    {
        // Reset the sort and filter
        SelectedSortIndex = DefaultSortIndex;
        Providers = new ObservableCollection<string> { _stringResource.GetLocalized("AllProviders") };
        SelectedProviderIndex = 0;
        _wasSyncButtonClicked = true;

        // Reset the old sync timer
        _cancellationTokenSource.Cancel();
        await _mainWindow.DispatcherQueue.EnqueueAsync(() => LastSyncTime = _stringResource.GetLocalized("MomentsAgo"));

        // We need to signal to the compute system manager that it can remove all the completed operations now that
        // we're done showing them in the view.
        _computeSystemManager.RemoveAllCompletedOperations();
        await LoadModelAsync();
        _wasSyncButtonClicked = false;
    }

    /// <summary>
    /// Navigates the user to the select environments page in the setup flow. This is the first page in the create environment
    /// process.
    /// </summary>
    [RelayCommand]
    public void CallToActionInvokeButton()
    {
        _log.Information("User clicked on the create environment button. Navigating to Select environment page in Setup flow");
        _navigationService.NavigateTo(KnownPageKeys.SetupFlow, "startCreationFlow;EnvironmentsLandingPage");
    }

    public void ConfigureComputeSystem(ComputeSystemReviewItem item)
    {
        _log.Information("User clicked on the setup button. Navigating to the Setup an Environment page in Setup flow");
        object[] parameters = { "StartConfigurationFlow", "EnvironmentsLandingPage", item };

        // Run on the UI thread
        _mainWindow.DispatcherQueue.EnqueueAsync(() =>
        {
            _navigationService.NavigateTo(KnownPageKeys.SetupFlow, parameters);
        });
    }

    // Updates the last sync time on the UI thread after set delay
    private async Task UpdateLastSyncTimeUI(string time, TimeSpan delay, CancellationToken token)
    {
        await Task.Delay(delay, token);

        if (!token.IsCancellationRequested)
        {
            await _mainWindow.DispatcherQueue.EnqueueAsync(() => LastSyncTime = time);
        }
    }

    private async Task RunSyncTimmer()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        await UpdateLastSyncTimeUI(_stringResource.GetLocalized("MinuteAgo"), TimeSpan.FromMinutes(1), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // For the first 2-5 minutes, in 1 minute increments
        for (var i = 2; i <= 5; i++)
        {
            await UpdateLastSyncTimeUI(_stringResource.GetLocalized("MinutesAgo", i), TimeSpan.FromMinutes(1), cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }

        // For the 10-55 minutes, in 5 minute increments
        for (var i = 2; i <= 11; i++)
        {
            await UpdateLastSyncTimeUI(_stringResource.GetLocalized("MinutesAgo", i * 5), TimeSpan.FromMinutes(5), cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }

        // For an hour and more
        await UpdateLastSyncTimeUI(_stringResource.GetLocalized("HourAgo"), TimeSpan.FromMinutes(5), cancellationToken);
    }

    /// <summary>
    /// Main entry point for loading the view model.
    /// </summary>
    public async Task LoadModelAsync()
    {
        lock (_lock)
        {
            if (IsLoading)
            {
                return;
            }

            // If the page has already loaded once, then we don't need to re-load the compute systems as that can take a while.
            // The user can click the sync button to refresh the compute systems. However, there may be new operations that have started
            // since the last time the page was loaded. So we need to add those to the view model quickly.
            // But if the user toggled extensions, we need to reload the page to show refreshed data.
            SetupCreateComputeSystemOperationForUI();
            if (HasPageLoadedForTheFirstTime && !_wasSyncButtonClicked && !_extensionsToggled)
            {
                return;
            }

            IsLoading = true;
        }

        // Start a new sync timer
        _ = Task.Run(async () =>
        {
            await RunSyncTimmer();
        });

        lock (ComputeSystemCards)
        {
            for (var i = ComputeSystemCards.Count - 1; i >= 0; i--)
            {
                if (ComputeSystemCards[i] is ComputeSystemViewModel computeSystemViewModel)
                {
                    computeSystemViewModel.RemoveStateChangedHandler();
                    ComputeSystemCards[i].ComputeSystemErrorReceived -= OnComputeSystemOperationError;
                    ComputeSystemCards.RemoveAt(i);
                }
            }
        }

        _notificationsHelper?.ClearNotifications();
        CallToActionText = null;
        CallToActionHyperLinkButtonText = null;
        ShouldNavigateToExtensionsPage = false;
        ShowLoadingShimmer = true;
        await _computeSystemManager.GetComputeSystemsAsync(AddAllComputeSystemsFromAProvider);
        ShowLoadingShimmer = false;
        UpdateCallToActionText();

        lock (_lock)
        {
            IsLoading = false;
            HasPageLoadedForTheFirstTime = true;
            _extensionsToggled = false;
        }
    }

    /// <summary>
    /// Sets up the view model to show the create compute system operations that the compute system manager contains.
    /// </summary>
    private void SetupCreateComputeSystemOperationForUI()
    {
        // Remove all the operations from view and then add the ones the manager has.
        _log.Information($"Adding any new create compute system operations to ComputeSystemCards list");
        var curOperations = _computeSystemManager.GetRunningOperationsForCreation();

        lock (ComputeSystemCards)
        {
            for (var i = ComputeSystemCards.Count - 1; i >= 0; i--)
            {
                if (ComputeSystemCards[i] is CreateComputeSystemOperationViewModel operationViewModel)
                {
                    operationViewModel!.RemoveEventHandlers();
                    operationViewModel.ComputeSystemErrorReceived -= OnComputeSystemOperationError;
                    ComputeSystemCards.RemoveAt(i);
                }
            }

            // Add new operations to the list
            foreach (var operation in curOperations)
            {
                // this is a new operation so we need to create a view model for it.
                var operationViewModel = new CreateComputeSystemOperationViewModel(
                    _computeSystemManager,
                    _stringResource,
                    _mainWindow,
                    RemoveComputeSystemCard,
                    AddNewlyCreatedComputeSystem,
                    operation);

                operationViewModel.ComputeSystemErrorReceived += OnComputeSystemOperationError;
                ComputeSystemCards.Insert(0, operationViewModel);
                _log.Information($"Found new create compute system operation for provider {operation.ProviderDetails.ComputeSystemProvider}, with name {operation.EnvironmentName}");
            }

            ComputeSystemCardsView.Refresh();
            UpdateCallToActionText();
        }
    }

    private async Task AddAllComputeSystemsFromAProvider(ComputeSystemsLoadedData data)
    {
        _notificationsHelper?.DisplayComputeSystemEnumerationErrors(data);
        var provider = data.ProviderDetails.ComputeSystemProvider;

        // List of ComputeSystemViewModels to be added to the view model
        // that didn't have any errors during initialization
        var computeSystemViewModels = new List<ComputeSystemViewModel>();
        foreach (var mapping in data.DevIdToComputeSystemMap.Where(map =>
            map.Value.Result.Status != ProviderOperationStatus.Failure))
        {
            var computeSystems = mapping.Value.ComputeSystems;

            // Initialize the cards for the compute systems in parallel before adding them to the view model on UI thread
            var packageFullName = data.ProviderDetails.ExtensionWrapper.PackageFullName;
            foreach (var computeSystem in computeSystems)
            {
                var computeSystemViewModel = new ComputeSystemViewModel(
                    _computeSystemManager,
                    computeSystem,
                    provider,
                    RemoveComputeSystemCard,
                    ConfigureComputeSystem,
                    packageFullName,
                    _mainWindow);

                computeSystemViewModel.ComputeSystemErrorReceived += OnComputeSystemOperationError;
                computeSystemViewModels.Add(computeSystemViewModel);
            }
        }

        await Parallel.ForEachAsync(computeSystemViewModels, async (computeSystemModel, token) =>
        {
            await computeSystemModel.InitializeCardDataAsync();
        });

        await _mainWindow.DispatcherQueue.EnqueueAsync(() =>
        {
            try
            {
                Providers.Add(provider.DisplayName);
                foreach (var computeSystemViewModel in computeSystemViewModels)
                {
                    computeSystemViewModel.InitializeUXData();
                    lock (ComputeSystemCards)
                    {
                        ComputeSystemCards.Add(computeSystemViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Exception occurred while adding Compute systems to environments page for provider: {provider.Id}");
            }
        });

        _computeSystemLoadWait.Set();
    }

    /// <summary>
    /// Updates the view model to show only the compute systems that match the search criteria.
    /// </summary>
    [RelayCommand]
    public void SearchHandler(string query)
    {
        ComputeSystemCardsView.Filter = system =>
        {
            if (system is CreateComputeSystemOperationViewModel createComputeSystemOperationViewModel)
            {
                var providerName = createComputeSystemOperationViewModel.ProviderDisplayName;
                if (providerName != _selectedProvider)
                {
                    return false;
                }

                return createComputeSystemOperationViewModel.EnvironmentName.Contains(query, StringComparison.OrdinalIgnoreCase);
            }

            if (system is ComputeSystemViewModel computeSystemViewModel)
            {
                if (_selectedProvider.Length > 0 && computeSystemViewModel.ProviderDisplayName != _selectedProvider)
                {
                    return false;
                }

                var systemName = computeSystemViewModel.ComputeSystem!.DisplayName.Value;
                var systemAltName = computeSystemViewModel.ComputeSystem.SupplementalDisplayName.Value;
                return systemName.Contains(query, StringComparison.OrdinalIgnoreCase) || systemAltName.Contains(query, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };
    }

    /// <summary>
    /// Updates the view model to filter the compute systems according to the provider.
    /// </summary>
    [RelayCommand]
    public void ProviderHandler(int selectedIndex)
    {
        SelectedProviderIndex = selectedIndex;
        _selectedProvider = Providers[SelectedProviderIndex];
        ComputeSystemCardsView.Filter = system =>
        {
            if (_selectedProvider.Equals(_stringResource.GetLocalized("AllProviders"), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (system is CreateComputeSystemOperationViewModel createComputeSystemOperationViewModel)
            {
                return createComputeSystemOperationViewModel.ProviderDisplayName.Equals(_selectedProvider, StringComparison.OrdinalIgnoreCase);
            }

            if (system is ComputeSystemViewModel computeSystemViewModel)
            {
                return computeSystemViewModel.ProviderDisplayName.Equals(_selectedProvider, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };
    }

    /// <summary>
    /// Updates the view model to sort the compute systems according to the sort criteria.
    /// </summary>
    /// <remarks>
    /// New SortDescription property names should be added as new properties to <see cref="ComputeSystemCardBase"/>
    /// </remarks>
    [RelayCommand]
    public void SortHandler()
    {
        ComputeSystemCardsView.SortDescriptions.Clear();

        if (SelectedSortIndex == (int)SortOptions.LastConnected)
        {
            ComputeSystemCardsView.SortDescriptions.Add(new SortDescription("IsCardCreating", SortDirection.Descending));
        }

        switch (SelectedSortIndex)
        {
            case (int)SortOptions.Alphabetical:
                ComputeSystemCardsView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
                break;
            case (int)SortOptions.AlphabeticalDescending:
                ComputeSystemCardsView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Descending));
                break;
            case (int)SortOptions.LastConnected:
                ComputeSystemCardsView.SortDescriptions.Add(new SortDescription("LastConnected", SortDirection.Ascending));
                break;
        }
    }

    private void AddNewlyCreatedComputeSystem(ComputeSystemViewModel computeSystemViewModel)
    {
        Task.Run(() =>
        {
            if (IsLoading)
            {
                _computeSystemLoadWait.WaitOne();
            }

            ComputeSystemCardBase? viewModel = default;
            lock (ComputeSystemCards)
            {
                viewModel = ComputeSystemCards.FirstOrDefault(viewBase => viewBase.ComputeSystemId.Equals(computeSystemViewModel.ComputeSystemId, StringComparison.OrdinalIgnoreCase));
            }

            if (viewModel == null)
            {
                _mainWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    lock (ComputeSystemCards)
                    {
                        computeSystemViewModel.ComputeSystemErrorReceived += OnComputeSystemOperationError;
                        ComputeSystemCards.Insert(0, computeSystemViewModel);
                    }

                    ComputeSystemCardsView.Refresh();
                });
            }
        });
    }

    private bool RemoveComputeSystemCard(ComputeSystemCardBase computeSystemCard)
    {
        lock (ComputeSystemCards)
        {
            return ComputeSystemCards.Remove(computeSystemCard);
        }
    }

    private void OnComputeSystemOperationError(ComputeSystemCardBase cardBase, string errorText)
    {
        _notificationsHelper?.DisplayComputeSystemOperationError(
            cardBase.ProviderDisplayName,
            cardBase.Name,
            errorText);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _computeSystemLoadWait.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void UpdateCallToActionText()
    {
        // if there are cards in the UI don't update the text and keep their values as null.
        if (ComputeSystemCards.Count > 0)
        {
            CallToActionText = null;
            return;
        }

        var providerCountWithOutAllKeyword = Providers.Count - 1;

        var callToActionData = ComputeSystemHelpers.UpdateCallToActionText(providerCountWithOutAllKeyword);
        ShouldNavigateToExtensionsPage = callToActionData.NavigateToExtensionsLibrary;
        CallToActionText = callToActionData.CallToActionText;
        CallToActionHyperLinkButtonText = callToActionData.CallToActionHyperLinkText;
    }
}
