// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

public partial class AppManagementViewModel : SetupPageViewModelBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppManagementViewModel));
    private readonly IScreenReaderService _screenReaderService;
    private readonly ShimmerSearchViewModel _shimmerSearchViewModel;
    private readonly SearchViewModel _searchViewModel;
    private readonly PackageCatalogListViewModel _packageCatalogListViewModel;
    private readonly SearchMessageViewModel _messageViewModel;
    private readonly PackageProvider _packageProvider;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private string _searchText;

    /// <summary>
    /// Current view to display in the main content control
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentView;

    [ObservableProperty]
    private bool _showInstalledPackageWarning;

    public ReadOnlyObservableCollection<PackageViewModel> SelectedPackages => _packageProvider.SelectedPackages;

    public string ApplicationsAddedText => SelectedPackages.Count == 1 ?
        StringResource.GetLocalized(StringResourceKey.ApplicationsAddedSingular) :
        StringResource.GetLocalized(StringResourceKey.ApplicationsAddedPlural, SelectedPackages.Count);

    public bool EnableRemoveAll => SelectedPackages.Count > 0;

    public AppManagementViewModel(
        ISetupFlowStringResource stringResource,
        IScreenReaderService screenReaderService,
        SetupFlowOrchestrator orchestrator,
        IHost host,
        PackageProvider packageProvider)
        : base(stringResource, orchestrator)
    {
        _screenReaderService = screenReaderService;
        _packageProvider = packageProvider;
        _searchViewModel = host.GetService<SearchViewModel>();
        _shimmerSearchViewModel = host.GetService<ShimmerSearchViewModel>();
        _packageCatalogListViewModel = host.GetService<PackageCatalogListViewModel>();
        _messageViewModel = host.GetService<SearchMessageViewModel>();
        _dispatcherQueue = host.GetService<DispatcherQueue>();

        PageTitle = StringResource.GetLocalized(StringResourceKey.ApplicationsPageTitle);

        SelectDefaultView();
    }

    protected async override Task OnEachNavigateToAsync()
    {
        SelectDefaultView();
        await Task.CompletedTask;
    }

    private void SelectDefaultView()
    {
        // By default, show the package catalogs
        CurrentView = _packageCatalogListViewModel;
    }

    private void SelectNoInternetView()
    {
        SelectMessageView(
            StringResource.GetLocalized(StringResourceKey.NoInternetConnectionTitle),
            StringResource.GetLocalized(StringResourceKey.NoInternetConnectionDescription));
    }

    private bool IsInternetAvailable()
    {
        return NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable;
    }

    private void SelectMessageView(string primaryMessage, string secondaryMessage)
    {
        _messageViewModel.PrimaryMessage = primaryMessage;
        _messageViewModel.SecondaryMessage = secondaryMessage;
        CurrentView = _messageViewModel;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SearchTextChangedAsync(string text, CancellationToken cancellationToken)
    {
        // Ensure internet is available before attempting to search
        if (!IsInternetAvailable())
        {
            SelectNoInternetView();
            return;
        }

        // Change view to searching
        CurrentView = _shimmerSearchViewModel;

        var searchResultStatus = await _searchViewModel.SearchAsync(text, cancellationToken);
        switch (searchResultStatus)
        {
            case SearchViewModel.SearchResultStatus.Ok:
                CurrentView = _searchViewModel;
                break;
            case SearchViewModel.SearchResultStatus.EmptySearchQuery:
                SelectDefaultView();
                break;
            case SearchViewModel.SearchResultStatus.CatalogNotConnect:
            case SearchViewModel.SearchResultStatus.ExceptionThrown:
                _log.Error($"Search failed with status: {searchResultStatus}");
                SelectDefaultView();
                break;
            case SearchViewModel.SearchResultStatus.Canceled:
            default:
                // noop
                break;
        }
    }

    [RelayCommand]
    private void RemoveAllPackages()
    {
        _log.Information($"Removing all packages from selected applications for installation");
        foreach (var package in SelectedPackages.ToList())
        {
            package.IsSelected = false;
        }

        _screenReaderService.Announce(StringResource.GetLocalized(StringResourceKey.RemovedAllApplications));
    }

    [RelayCommand]
    private void OnLoaded()
    {
        if (!IsInternetAvailable())
        {
            SelectNoInternetView();
        }

        _packageProvider.SelectedPackagesItemChanged += OnPackageSelectionChanged;
        NetworkHelper.Instance.NetworkChanged += OnNetworkChanged;
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        _packageProvider.SelectedPackagesItemChanged -= OnPackageSelectionChanged;
        NetworkHelper.Instance.NetworkChanged -= OnNetworkChanged;
    }

    private void OnNetworkChanged(object sender, EventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (IsInternetAvailable())
            {
                SelectDefaultView();
            }
            else
            {
                SelectNoInternetView();
            }
        });
    }

    private void OnPackageSelectionChanged(object sender, EventArgs args)
    {
        // Notify UI to update
        OnPropertyChanged(nameof(ApplicationsAddedText));
        OnPropertyChanged(nameof(EnableRemoveAll));

        // Show warning if any selected package is installed
        ShowInstalledPackageWarning = SelectedPackages.Any(p => !p.CanInstall);
    }

    internal void PerformSearch(string searchParameter)
    {
        SearchText = searchParameter;
    }
}
