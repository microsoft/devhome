// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Environments.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using WinUIEx;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// The main view model for the landing page of the Environments tool.
/// </summary>
public partial class LandingPageViewModel : ObservableObject, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(LandingPageViewModel));

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly EnvironmentsExtensionsService _extensionsService;

    private readonly NotificationService _notificationService;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly StringResource _stringResource;

    private readonly object _lock = new();

    public bool IsLoading { get; set; }

    public ObservableCollection<ComputeSystemViewModel> ComputeSystems { get; set; } = new();

    public AdvancedCollectionView ComputeSystemsView { get; set; }

    public bool HasPageLoadedForTheFirstTime { get; set; }

    [ObservableProperty]
    private bool _showLoadingShimmer = true;

    [ObservableProperty]
    private int _selectedProviderIndex;

    [ObservableProperty]
    private int _selectedSortIndex;

    [ObservableProperty]
    private string _lastSyncTime;

    public ObservableCollection<string> Providers { get; set; }

    private CancellationTokenSource _cancellationTokenSource = new();

    public LandingPageViewModel(
                IComputeSystemManager manager,
                EnvironmentsExtensionsService extensionsService,
                NotificationService notificationService)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _extensionsService = extensionsService;
        _notificationService = notificationService;
        _computeSystemManager = manager;
        _stringResource = new StringResource("DevHome.Environments.pri", "DevHome.Environments/Resources");

        SelectedSortIndex = -1;
        Providers = new() { _stringResource.GetLocalized("AllProviders") };
        _lastSyncTime = _stringResource.GetLocalized("MomentsAgo");

        ComputeSystemsView = new AdvancedCollectionView(ComputeSystems);
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationService.Initialize(notificationQueue);

        // To Do: Need to give the users a way to disable this, if they don't want to use Hyper-V
        _ = Task.Run(() => _notificationService.CheckIfUserIsAHyperVAdminAndShowNotification());
    }

    [RelayCommand]
    public async Task SyncButton()
    {
        // Reset the sort and filter
        SelectedSortIndex = -1;
        Providers = new ObservableCollection<string> { _stringResource.GetLocalized("AllProviders") };
        SelectedProviderIndex = 0;

        // Reset the old sync timer
        _cancellationTokenSource.Cancel();
        await _dispatcher.EnqueueAsync(() => LastSyncTime = _stringResource.GetLocalized("MomentsAgo"));

        await LoadModelAsync();

        // Start a new sync timer
        _ = Task.Run(async () =>
        {
            await RunSyncTimmer();
        });
    }

    // Updates the last sync time on the UI thread after set delay
    private async Task UpdateLastSyncTimeUI(string time, TimeSpan delay, CancellationToken token)
    {
        await Task.Delay(delay, token);

        if (!token.IsCancellationRequested)
        {
            await _dispatcher.EnqueueAsync(() => LastSyncTime = time);
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
    public async Task LoadModelAsync(bool useDebugValues = false)
    {
        lock (_lock)
        {
            if (IsLoading)
            {
                return;
            }

            HasPageLoadedForTheFirstTime = true;
            IsLoading = true;
        }

        // Start a new sync timer
        _ = Task.Run(async () =>
        {
            await RunSyncTimmer();
        });

        for (var i = ComputeSystems.Count - 1; i >= 0; i--)
        {
            ComputeSystems[i].RemoveStateChangedHandler();
            ComputeSystems.RemoveAt(i);
        }

        ShowLoadingShimmer = true;
        await _extensionsService.GetComputeSystemsAsync(useDebugValues, AddAllComputeSystemsFromAProvider);
        ShowLoadingShimmer = false;

        lock (_lock)
        {
            IsLoading = false;
        }
    }

    private async Task AddAllComputeSystemsFromAProvider(ComputeSystemsLoadedData data)
    {
        var provider = data.ProviderDetails.ComputeSystemProvider;

        // Show error notifications for failed provider/developer id combinations
        foreach (var mapping in data.DevIdToComputeSystemMap.Where(kv =>
            kv.Value.Result.Status == Microsoft.Windows.DevHome.SDK.ProviderOperationStatus.Failure))
        {
            var result = mapping.Value.Result;
            await _notificationService.ShowNotificationAsync(provider.DisplayName, result.DisplayMessage, InfoBarSeverity.Error);

            _log.Error($"Error occurred while adding Compute systems to environments page for provider: {provider.Id}", result.DiagnosticText, result.ExtendedError);
            data.DevIdToComputeSystemMap.Remove(mapping.Key);
        }

        await _dispatcher.EnqueueAsync(async () =>
        {
            Providers.Add(provider.DisplayName);
            try
            {
                var computeSystemList = data.DevIdToComputeSystemMap.Values.SelectMany(x => x.ComputeSystems).ToList();

                // In the future when we support switching between accounts in the environments page, we will need to handle this differently.
                // for now we'll show all the compute systems from a provider.
                if (computeSystemList == null || computeSystemList.Count == 0)
                {
                    _log.Error($"No Compute systems found for provider: {provider.Id}");
                    return;
                }

                for (var i = 0; i < computeSystemList.Count; i++)
                {
                    var packageFullName = data.ProviderDetails.ExtensionWrapper.PackageFullName;
                    var computeSystemViewModel = new ComputeSystemViewModel(_computeSystemManager, computeSystemList.ElementAt(i), provider, packageFullName);
                    await computeSystemViewModel.InitializeCardDataAsync();
                    ComputeSystems.Add(computeSystemViewModel);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception occurred while adding Compute systems to environments page for provider: {provider.Id}", ex);
            }
        });
    }

    /// <summary>
    /// Updates the view model to show only the compute systems that match the search criteria.
    /// </summary>
    [RelayCommand]
    public void SearchHandler(string query)
    {
        ComputeSystemsView.Filter = system =>
        {
            if (system is ComputeSystemViewModel computeSystemViewModel)
            {
                var systemName = computeSystemViewModel.ComputeSystem.DisplayName;
                var systemAltName = computeSystemViewModel.ComputeSystem.SupplementalDisplayName;
                return systemName.Contains(query, StringComparison.OrdinalIgnoreCase) || systemAltName.Contains(query, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };
    }

    /// <summary>
    /// Updates the view model to filter the compute systems according to the provider.
    /// </summary>
    [RelayCommand]
    public void ProviderHandler()
    {
        var currentProvider = Providers[SelectedProviderIndex];
        ComputeSystemsView.Filter = system =>
        {
            if (currentProvider.Equals(_stringResource.GetLocalized("AllProviders"), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (system is ComputeSystemViewModel computeSystemViewModel)
            {
                var type = computeSystemViewModel.Type;
                return type.Equals(currentProvider, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };
    }

    /// <summary>
    /// Updates the view model to sort the compute systems according to the sort criteria.
    /// </summary>
    [RelayCommand]
    public void SortHandler()
    {
        ComputeSystemsView.SortDescriptions.Clear();

        switch (SelectedSortIndex)
        {
            case 0:
                ComputeSystemsView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
                break;
            case 1:
                ComputeSystemsView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Descending));
                break;
            case 2:
                ComputeSystemsView.SortDescriptions.Add(new SortDescription("AlternativeName", SortDirection.Ascending));
                break;
            case 3:
                ComputeSystemsView.SortDescriptions.Add(new SortDescription("AlternativeName", SortDirection.Descending));
                break;
            case 4:
                ComputeSystemsView.SortDescriptions.Add(new SortDescription("LastConnected", SortDirection.Ascending));
                break;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
