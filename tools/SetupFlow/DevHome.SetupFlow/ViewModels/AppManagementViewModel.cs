// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;

namespace DevHome.SetupFlow.ViewModels;

public partial class AppManagementViewModel : SetupPageViewModelBase
{
    private readonly ShimmerSearchViewModel _shimmerSearchViewModel;
    private readonly SearchViewModel _searchViewModel;
    private readonly PackageCatalogListViewModel _packageCatalogListViewModel;
    private readonly IWindowsPackageManager _wpm;
    private readonly PackageProvider _packageProvider;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    /// Current view to display in the main content control
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentView;

    [ObservableProperty]
    private bool _searchBoxEnabled;

    public ReadOnlyObservableCollection<PackageViewModel> SelectedPackages => _packageProvider.SelectedPackages;

    public string ApplicationsAddedText => SelectedPackages.Count == 1 ?
        StringResource.GetLocalized(StringResourceKey.ApplicationsAddedSingular) :
        StringResource.GetLocalized(StringResourceKey.ApplicationsAddedPlural, SelectedPackages.Count);

    public bool EnableRemoveAll => SelectedPackages.Count > 0;

    public AppManagementViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host,
        IWindowsPackageManager wpm,
        PackageProvider packageProvider)
        : base(stringResource, orchestrator)
    {
        _wpm = wpm;
        _packageProvider = packageProvider;
        _searchViewModel = host.GetService<SearchViewModel>();
        _shimmerSearchViewModel = host.GetService<ShimmerSearchViewModel>();
        _packageCatalogListViewModel = host.GetService<PackageCatalogListViewModel>();

        _packageProvider.PackageSelectionChanged += (_, _) => OnPropertyChanged(nameof(ApplicationsAddedText));
        _packageProvider.PackageSelectionChanged += (_, _) => OnPropertyChanged(nameof(EnableRemoveAll));

        PageTitle = StringResource.GetLocalized(StringResourceKey.ApplicationsPageTitle);

        SelectDefaultView();
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        // Load catalogs from all data sources
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Loading package catalogs from all sources");
        var loadCatalogs = _packageCatalogListViewModel.LoadCatalogsAsync();

        // Connect to composite catalog used for searching on a separate
        // (non-UI) thread to prevent lagging the UI.
        var allCatalogsConnect = Task.Run(async () =>
        {
            try
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, "Connecting to composite catalog to enable searching for packages");
                await Task.Run(async () => await _wpm.AllCatalogs.ConnectAsync());

                // Enable search box after catalog connection is complete
                _dispatcherQueue.TryEnqueue(() => SearchBoxEnabled = _wpm.AllCatalogs.IsConnected);
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, "Failed to connect to composite catalog to  enable searching. Search will be disabled.", e);
            }
        });

        await Task.WhenAll(allCatalogsConnect, loadCatalogs);
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

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SearchTextChangedAsync(string text, CancellationToken cancellationToken)
    {
        // Change view to searching
        CurrentView = _shimmerSearchViewModel;

        var (searchResultStatus, _) = await _searchViewModel.SearchAsync(text, cancellationToken);
        switch (searchResultStatus)
        {
            case SearchViewModel.SearchResultStatus.Ok:
                CurrentView = _searchViewModel;
                break;
            case SearchViewModel.SearchResultStatus.EmptySearchQuery:
                CurrentView = _packageCatalogListViewModel;
                break;
            case SearchViewModel.SearchResultStatus.CatalogNotConnect:
            case SearchViewModel.SearchResultStatus.ExceptionThrown:
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Search failed with status: {searchResultStatus}");
                CurrentView = _packageCatalogListViewModel;
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
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Removing all packages from selected applications for installation");
        foreach (var package in SelectedPackages.ToList())
        {
            package.IsSelected = false;
        }
    }
}
