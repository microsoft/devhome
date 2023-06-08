// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Dashboard.ViewModels;
using DevHome.Settings.ViewModels;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly SetupFlowOrchestrator _orchestrator;
    private readonly SetupFlowViewModel _setupFlowViewModel;
    private readonly IHost _host;
    private readonly Lazy<IList<ConfigurationUnitResultViewModel>> _configurationUnitResults;
    private readonly ConfigurationUnitResultViewModelFactory _configurationUnitResultViewModelFactory;
    private readonly IWindowsPackageManager _wpm;
    private readonly PackageProvider _packageProvider;
    private readonly CatalogDataSourceLoacder _catalogDataSourceLoacder;

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    public ObservableCollection<RepoViewListItem> RepositoriesCloned
    {
        get
        {
            var repositoriesCloned = new ObservableCollection<RepoViewListItem>();
            var taskGroup = _host.GetService<SetupFlowOrchestrator>().TaskGroups;
            var group = taskGroup.SingleOrDefault(x => x.GetType() == typeof(RepoConfigTaskGroup));
            if (group is RepoConfigTaskGroup repoTaskGroup)
            {
                foreach (var task in repoTaskGroup.SetupTasks)
                {
                    if (task is CloneRepoTask repoTask && repoTask.WasCloningSuccessful)
                    {
                        repositoriesCloned.Add(new (repoTask.RepositoryToClone));
                    }
                }
            }

            var localizedHeader = (repositoriesCloned.Count == 1) ? StringResourceKey.SummaryPageOneRepositoryCloned : StringResourceKey.SummaryPageReposClonedCount;
            RepositoriesClonedText = StringResource.GetLocalized(localizedHeader);
            return repositoriesCloned;
        }
    }

    public ObservableCollection<PackageViewModel> AppsDownloaded
    {
        get
        {
            var packagesInstalled = new ObservableCollection<PackageViewModel>();
            var packages = _packageProvider.SelectedPackages.Where(sp => sp.InstallPackageTask.WasInstallSuccessful == true);
            foreach (var package in packages)
            {
                packagesInstalled.Add(package);
            }

            var localizedHeader = (packagesInstalled.Count == 1) ? StringResourceKey.SummaryPageOneApplicationInstalled : StringResourceKey.SummaryPageAppsDownloadedCount;
            ApplicationsClonedText = StringResource.GetLocalized(localizedHeader);
            return packagesInstalled;
        }
    }

    public IList<ConfigurationUnitResultViewModel> ConfigurationUnitResults => _configurationUnitResults.Value;

    public bool ShowConfigurationUnitResults => ConfigurationUnitResults.Any();

    public bool CompletedWithErrors => ConfigurationUnitResults.Any(unitResult => unitResult.IsError);

    public int ConfigurationUnitSucceededCount => ConfigurationUnitResults.Count(unitResult => unitResult.IsSuccess);

    public int ConfigurationUnitFailedCount => ConfigurationUnitResults.Count(unitResult => unitResult.IsError);

    public int ConfigurationUnitSkippedCount => ConfigurationUnitResults.Count(unitResult => unitResult.IsSkipped);

    public string ConfigurationUnitStats => StringResource.GetLocalized(
        StringResourceKey.ConfigurationUnitStats,
        ConfigurationUnitSucceededCount,
        ConfigurationUnitFailedCount,
        ConfigurationUnitSkippedCount);

    [ObservableProperty]
    private string _repositoriesClonedText;

    [ObservableProperty]
    private string _applicationsClonedText;

    [RelayCommand]
    public void RemoveRestartGrid()
    {
        ShowRestartNeeded = Visibility.Collapsed;
    }

    /// <summary>
    /// Method here for telemetry
    /// </summary>
    [RelayCommand]
    public async void LearnMore()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Measure, new NavigateFromSummaryEvent("LearnMoreAboutDevHome"));
        await Launcher.LaunchUriAsync(new Uri("https://learn.microsoft.com/windows/"));
    }

    [RelayCommand]
    public void GoToMainPage()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Measure, new NavigateFromSummaryEvent("MachineConfiguration"));
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToMainPage");
    }

    [RelayCommand]
    public void GoToDashboard()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Measure, new NavigateFromSummaryEvent("Dashboard"));
        _host.GetService<INavigationService>().NavigateTo(typeof(DashboardViewModel).FullName);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToDashboard");
    }

    [RelayCommand]
    public void GoToDevHomeSettings()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Measure, new NavigateFromSummaryEvent("DevHomeSettings"));
        _host.GetService<INavigationService>().NavigateTo(typeof(SettingsViewModel).FullName);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToSettings");
    }

    [RelayCommand]
    public void GoToForDevelopersSettingsPage()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Measure, new NavigateFromSummaryEvent("WindowsDeveloperSettings"));
        Task.Run(() => Launcher.LaunchUriAsync(new Uri("ms-settings:developers"))).Wait();
    }

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        SetupFlowViewModel setupFlowViewModel,
        IHost host,
        ConfigurationUnitResultViewModelFactory configurationUnitResultViewModelFactory,
        IWindowsPackageManager wpm,
        PackageProvider packageProvider,
        CatalogDataSourceLoacder catalogDataSourceLoader)
        : base(stringResource, orchestrator)
    {
        _orchestrator = orchestrator;
        _setupFlowViewModel = setupFlowViewModel;
        _host = host;
        _configurationUnitResultViewModelFactory = configurationUnitResultViewModelFactory;
        _wpm = wpm;
        _packageProvider = packageProvider;
        _catalogDataSourceLoacder = catalogDataSourceLoader;
        IsNavigationBarVisible = true;
        _configurationUnitResults = new (GetConfigurationUnitResults);
        _showRestartNeeded = Visibility.Collapsed;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        TelemetryFactory.Get<ITelemetry>().LogMeasure("Summary_NavigatedTo_Event");
        _orchestrator.ReleaseRemoteFactory();
        await ReloadCatalogsAsync();
    }

    private async Task ReloadCatalogsAsync()
    {
        // After installing packages, reconnect to catalogs to
        // reflect the latest changes when new Package COM objects are created
        Log.Logger?.ReportInfo(Log.Component.Summary, $"Checking if a new catalog connections should be established");
        if (_packageProvider.SelectedPackages.Any(package => package.InstallPackageTask.WasInstallSuccessful))
        {
            await Task.Run(async () =>
            {
                Log.Logger?.ReportInfo(Log.Component.Summary, $"Creating a new catalog connections");
                await _wpm.ConnectToAllCatalogsAsync(force: true);

                Log.Logger?.ReportInfo(Log.Component.Summary, $"Reloading catalogs from all data sources");
                _catalogDataSourceLoacder.Clear();
                await foreach (var dataSourceCatalogs in _catalogDataSourceLoacder.LoadCatalogsAsync())
                {
                    Log.Logger?.ReportInfo(Log.Component.Summary, $"Reloaded {dataSourceCatalogs.Count} catalog(s)");
                }
            });
        }
    }

    /// <summary>
    /// Get the list of configuration unit results for an applied
    /// configuration file task.
    /// </summary>
    /// <returns>List of configuration unit result</returns>
    private IList<ConfigurationUnitResultViewModel> GetConfigurationUnitResults()
    {
        List<ConfigurationUnitResultViewModel> unitResults = new ();
        var configTaskGroup = _orchestrator.GetTaskGroup<ConfigurationFileTaskGroup>();
        if (configTaskGroup?.ConfigureTask?.UnitResults != null)
        {
            unitResults.AddRange(configTaskGroup.ConfigureTask.UnitResults.Select(unitResult => _configurationUnitResultViewModelFactory(unitResult)));
        }

        return unitResults;
    }
}
