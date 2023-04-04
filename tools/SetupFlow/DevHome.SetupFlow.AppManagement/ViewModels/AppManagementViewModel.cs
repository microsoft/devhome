// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

public partial class AppManagementViewModel : SetupPageViewModelBase
{
    private readonly ShimmerSearchViewModel _shimmerSearchViewModel;
    private readonly SearchViewModel _searchViewModel;
    private readonly PackageCatalogListViewModel _packageCatalogListViewModel;
    private readonly IWindowsPackageManager _wpm;
    private readonly PackageProvider _packageProvider;

    /// <summary>
    /// Current view to display in the main content control
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentView;

    public ReadOnlyObservableCollection<PackageViewModel> SelectedPackages => _packageProvider.SelectedPackages;

    /// <summary>
    /// Gets the localized string for <see cref="StringResourceKey.ApplicationsSelectedCount"/>
    /// </summary>
    public string ApplicationsSelectedCountText => StringResource.GetLocalized(StringResourceKey.ApplicationsSelectedCount, SelectedPackages.Count);

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

        _packageProvider.PackageSelectionChanged += (_, _) => OnPropertyChanged(nameof(ApplicationsSelectedCountText));

        PageTitle = StringResource.GetLocalized(StringResourceKey.ApplicationsPageTitle);

        // By default, show the package catalogs
        CurrentView = _packageCatalogListViewModel;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        // Load catalogs from all data sources
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Loading package catalogs from all sources");
        await _packageCatalogListViewModel.LoadCatalogsAsync();

        // Connect to composite catalog used for searching on a separate
        // (non-UI) thread to prevent lagging the UI.
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Connecting to composite catalog to enable searching for packages");
        await Task.Run(async () => await _wpm.AllCatalogs.ConnectAsync());
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
}
