// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

public partial class AppManagementViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly ShimmerSearchViewModel _shimmerSearchViewModel;
    private readonly SearchViewModel _searchViewModel;
    private readonly PackageCatalogListViewModel _packageCatalogListViewModel;
    private readonly AppManagementTaskGroup _taskGroup;
    private readonly IWindowsPackageManager _wpm;

    /// <summary>
    /// Current view to display in the main content control
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentView;

    public ObservableCollection<PackageViewModel> SelectedPackages { get; } = new ();

    /// <summary>
    /// Gets the localized string for <see cref="StringResourceKey.ApplicationsSelectedCount"/>
    /// </summary>
    public string ApplicationsSelectedCountText => StringResource.GetLocalized(StringResourceKey.ApplicationsSelectedCount, SelectedPackages.Count);

    public AppManagementViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        ILogger logger,
        IHost host,
        IWindowsPackageManager wpm,
        AppManagementTaskGroup taskGroup)
        : base(stringResource, orchestrator)
    {
        _logger = logger;
        _taskGroup = taskGroup;
        _wpm = wpm;

        _searchViewModel = host.GetService<SearchViewModel>();
        _shimmerSearchViewModel = host.GetService<ShimmerSearchViewModel>();

        _packageCatalogListViewModel = host.GetService<PackageCatalogListViewModel>();
        _packageCatalogListViewModel.CatalogLoaded += OnCatalogLoaded;

        PageTitle = StringResource.GetLocalized(StringResourceKey.ApplicationsPageTitle);

        // By default, show the package catalogs
        CurrentView = _packageCatalogListViewModel;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        // Load catalogs from all data sources
        await _packageCatalogListViewModel.LoadCatalogsAsync();

        // Connect to composite catalog used for searching on a separate
        // (non-UI) thread to prevent lagging the UI.
        await Task.Run(async () => await _wpm.AllCatalogs.ConnectAsync());
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SearchTextChangedAsync(string text, CancellationToken cancellationToken)
    {
        // Change view to searching
        CurrentView = _shimmerSearchViewModel;

        var (searchResultStatus, packages) = await _searchViewModel.SearchAsync(text, cancellationToken);
        switch (searchResultStatus)
        {
            case SearchViewModel.SearchResultStatus.Ok:
                CurrentView = _searchViewModel;
                SetPackageSelectionChangedHandler(packages);
                break;
            case SearchViewModel.SearchResultStatus.EmptySearchQuery:
                CurrentView = _packageCatalogListViewModel;
                break;
            case SearchViewModel.SearchResultStatus.CatalogNotConnect:
            case SearchViewModel.SearchResultStatus.ExceptionThrown:
                _logger.LogError(nameof(AppManagementViewModel), LogLevel.Local, $"Search failed with status: {searchResultStatus}");
                CurrentView = _packageCatalogListViewModel;
                break;
            case SearchViewModel.SearchResultStatus.Canceled:
            default:
                // noop
                break;
        }
    }

    private void SetPackageSelectionChangedHandler(List<PackageViewModel> packages)
    {
        foreach (var package in packages)
        {
            package.SelectionChanged += OnPackageSelectionChanged;
        }
    }

    private void OnCatalogLoaded(object sender, PackageCatalogViewModel packageCatalog)
    {
        packageCatalog.PackageSelectionChanged += OnPackageSelectionChanged;
    }

    private void OnPackageSelectionChanged(object sender, PackageViewModel package)
    {
        if (package.IsSelected)
        {
            SelectedPackages.Add(package);
        }
        else
        {
            SelectedPackages.Remove(package);
        }

        OnPropertyChanged(nameof(ApplicationsSelectedCountText));
    }
}
