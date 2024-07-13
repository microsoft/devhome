// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Contracts.Services;
using DevHome.Dashboard.ViewModels;
using DevHome.Settings.ViewModels;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private static readonly BitmapImage DarkError = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkError.png"));
    private static readonly BitmapImage LightError = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightError.png"));

    private readonly SetupFlowOrchestrator _orchestrator;
    private readonly SetupFlowViewModel _setupFlowViewModel;
    private readonly IHost _host;
    private readonly Lazy<IList<ConfigurationUnitResultViewModel>> _configurationUnitResults;
    private readonly ConfigurationUnitResultViewModelFactory _configurationUnitResultViewModelFactory;
    private readonly PackageProvider _packageProvider;
    private readonly IAppManagementInitializer _appManagementInitializer;

    [ObservableProperty]
    private List<SummaryErrorMessageViewModel> _failedTasks = new();

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    [RelayCommand]
    public async Task ShowLogFiles()
    {
        await Task.Run(() =>
        {
            var folderToOpen = Log.Logger.Options.LogFileFolderPath;
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.FileName = folderToOpen;
            var explorerWindow = new Process();
            explorerWindow.StartInfo = startInfo;
            explorerWindow.Start();
        });
    }

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
                        repositoriesCloned.Add(new(repoTask.RepositoryToClone));
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
            var packages = _packageProvider.SelectedPackages.Where(sp => sp.InstallPackageTask.WasInstallSuccessful == true).ToList();
            packages.ForEach(p => packagesInstalled.Add(p));
            var localizedHeader = (packagesInstalled.Count == 1) ? StringResourceKey.SummaryPageOneApplicationInstalled : StringResourceKey.SummaryPageAppsDownloadedCount;
            ApplicationsClonedText = StringResource.GetLocalized(localizedHeader);
            return packagesInstalled;
        }
    }

    public List<PackageViewModel> AppsDownloadedInstallationNotes => AppsDownloaded.Where(p => !string.IsNullOrEmpty(p.InstallationNotes)).ToList();

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
    public async Task LearnMoreAsync()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("LearnMoreAboutDevHome"), Orchestrator.ActivityId);
        await Launcher.LaunchUriAsync(new Uri("https://learn.microsoft.com/windows/"));
    }

    [RelayCommand]
    public void GoToMainPage()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("MachineConfiguration"), Orchestrator.ActivityId);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToMainPage");
    }

    [RelayCommand]
    public void GoToDashboard()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("Dashboard"), Orchestrator.ActivityId);
        _host.GetService<INavigationService>().NavigateTo(typeof(DashboardViewModel).FullName);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToDashboard");
    }

    [RelayCommand]
    public void GoToDevHomeSettings()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("DevHomeSettings"), Orchestrator.ActivityId);
        _host.GetService<INavigationService>().NavigateTo(typeof(SettingsViewModel).FullName);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToSettings");
    }

    [RelayCommand]
    public void GoToForDevelopersSettingsPage()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("WindowsDeveloperSettings"), Orchestrator.ActivityId);
        Task.Run(() => Launcher.LaunchUriAsync(new Uri("ms-settings:developers"))).Wait();
    }

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        SetupFlowViewModel setupFlowViewModel,
        IHost host,
        ConfigurationUnitResultViewModelFactory configurationUnitResultViewModelFactory,
        IAppManagementInitializer appManagementInitializer,
        PackageProvider packageProvider)
        : base(stringResource, orchestrator)
    {
        _orchestrator = orchestrator;
        _setupFlowViewModel = setupFlowViewModel;
        _host = host;
        _configurationUnitResultViewModelFactory = configurationUnitResultViewModelFactory;
        _packageProvider = packageProvider;
        _configurationUnitResults = new(GetConfigurationUnitResults);
        _showRestartNeeded = Visibility.Collapsed;
        _appManagementInitializer = appManagementInitializer;

        IsNavigationBarVisible = true;
        IsStepPage = false;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        IList<TaskInformation> failedTasks = new List<TaskInformation>();

        // Find the loading view model.
        foreach (var flowPage in _orchestrator.FlowPages)
        {
            if (flowPage is LoadingViewModel loadingViewModel)
            {
                failedTasks = loadingViewModel.FailedTasks;
            }
        }

        BitmapImage statusSymbol;
        if (_host.GetService<IThemeSelectorService>().Theme == ElementTheme.Dark)
        {
            statusSymbol = DarkError;
        }
        else
        {
            statusSymbol = LightError;
        }

        foreach (var failedTask in failedTasks)
        {
            var summaryMessageViewModel = new SummaryErrorMessageViewModel();
            summaryMessageViewModel.MessageToShow = failedTask.MessageToShow;
            summaryMessageViewModel.StatusSymbolIcon = statusSymbol;
            FailedTasks.Add(summaryMessageViewModel);
        }

        // If any tasks failed in the loading screen, the user has to click on the "Next" button
        // If no tasks failed, the user is brought to the summary screen, no interaction required.
        if (failedTasks.Count != 0)
        {
            TelemetryFactory.Get<ITelemetry>().LogCritical("Summary_NavigatedTo_Event", false, Orchestrator.ActivityId);
        }

        await ReloadCatalogsAsync();
    }

    private async Task ReloadCatalogsAsync()
    {
        // After installing packages, reconnect to catalogs to
        // reflect the latest changes when new Package COM objects are created
        Log.Logger?.ReportInfo(Log.Component.Summary, $"Checking if a new catalog connections should be established");
        if (_packageProvider.SelectedPackages.Any(package => package.InstallPackageTask.WasInstallSuccessful))
        {
            await _appManagementInitializer.ReinitializeAsync();
        }
    }

    /// <summary>
    /// Get the list of configuration unit results for an applied
    /// configuration file task.
    /// </summary>
    /// <returns>List of configuration unit result</returns>
    private List<ConfigurationUnitResultViewModel> GetConfigurationUnitResults()
    {
        List<ConfigurationUnitResultViewModel> unitResults = new();

        // If we are setting up a target machine, we need to get the configuration results from the setup target task group.
        if (_orchestrator.IsSettingUpATargetMachine)
        {
            var setupTaskGroup = _orchestrator.GetTaskGroup<SetupTargetTaskGroup>();
            if (setupTaskGroup?.ConfigureTask?.ConfigurationResults != null)
            {
                unitResults.AddRange(setupTaskGroup.ConfigureTask.ConfigurationResults.Select(unitResult => _configurationUnitResultViewModelFactory(unitResult)));
            }

            return unitResults;
        }

        var configTaskGroup = _orchestrator.GetTaskGroup<ConfigurationFileTaskGroup>();
        if (configTaskGroup?.ConfigureTask?.UnitResults != null)
        {
            unitResults.AddRange(configTaskGroup.ConfigureTask.UnitResults.Select(unitResult => _configurationUnitResultViewModelFactory(unitResult)));
        }

        return unitResults;
    }
}
