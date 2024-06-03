// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

public partial class AppManagementViewModel : SetupPageViewModelBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppManagementViewModel));
    private readonly ShimmerSearchViewModel _shimmerSearchViewModel;
    private readonly SearchViewModel _searchViewModel;
    private readonly PackageCatalogListViewModel _packageCatalogListViewModel;
    private readonly PackageProvider _packageProvider;

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
        SetupFlowOrchestrator orchestrator,
        IHost host,
        PackageProvider packageProvider)
        : base(stringResource, orchestrator)
    {
        _packageProvider = packageProvider;
        _searchViewModel = host.GetService<SearchViewModel>();
        _shimmerSearchViewModel = host.GetService<ShimmerSearchViewModel>();
        _packageCatalogListViewModel = host.GetService<PackageCatalogListViewModel>();

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
                _log.Error($"Search failed with status: {searchResultStatus}");
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
        _log.Information($"Removing all packages from selected applications for installation");
        foreach (var package in SelectedPackages.ToList())
        {
            package.IsSelected = false;
        }
    }

    [RelayCommand]
    private void OnLoaded()
    {
        _packageProvider.SelectedPackagesItemChanged += OnPackageSelectionChanged;
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        _packageProvider.SelectedPackagesItemChanged -= OnPackageSelectionChanged;
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
