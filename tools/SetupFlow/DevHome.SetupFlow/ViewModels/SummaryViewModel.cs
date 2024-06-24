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
using DevHome.Common.TelemetryEvents.SetupFlow.SummaryPage;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Views;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private static readonly BitmapImage DarkError = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DarkError.png"));
    private static readonly BitmapImage LightError = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/LightError.png"));

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SummaryViewModel));
    private readonly SetupFlowOrchestrator _orchestrator;
    private readonly SetupFlowViewModel _setupFlowViewModel;
    private readonly IHost _host;
    private readonly ConfigurationUnitResultViewModelFactory _configurationUnitResultViewModelFactory;
    private readonly PackageProvider _packageProvider;
    private readonly IAppManagementInitializer _appManagementInitializer;

    private readonly List<UserControl> _cloneRepoNextSteps;

    // Holds all the UI to display for "Next Steps".
    public List<UserControl> NextSteps => _cloneRepoNextSteps;

    [ObservableProperty]
    private ObservableCollection<ISummaryInformationViewModel> _summaryInformation;

    [ObservableProperty]
    private List<SummaryErrorMessageViewModel> _failedTasks = new();

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    [ObservableProperty]
    private string _targetFailedCountText;

    [RelayCommand]
    public async Task ShowLogFiles()
    {
        await Task.Run(() =>
        {
            var folderToOpen = DevHome.Common.Logging.LogFolderRoot;
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
            if (!IsSettingUpATargetMachine)
            {
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
            }

            return repositoriesCloned;
        }
    }

    public ObservableCollection<PackageViewModel> AppsDownloaded
    {
        get
        {
            var packagesInstalled = new ObservableCollection<PackageViewModel>();
            if (!IsSettingUpATargetMachine)
            {
                var packages = _packageProvider.SelectedPackages.Where(sp => sp.CanInstall && sp.InstallPackageTask.WasInstallSuccessful).ToList();
                packages.ForEach(p => packagesInstalled.Add(p));
                var localizedHeader = (packagesInstalled.Count == 1) ? StringResourceKey.SummaryPageOneApplicationInstalled : StringResourceKey.SummaryPageAppsDownloadedCount;
                ApplicationsClonedText = StringResource.GetLocalized(localizedHeader);
            }

            return packagesInstalled;
        }
    }

    public bool WasCreateEnvironmentOperationStarted
    {
        get
        {
            var taskGroup = Orchestrator.GetTaskGroup<EnvironmentCreationOptionsTaskGroup>();
            if (taskGroup == null)
            {
                return false;
            }

            return taskGroup.CreateEnvironmentTask.CreationOperationStarted;
        }
    }

    public List<PackageViewModel> AppsDownloadedInstallationNotes => AppsDownloaded.Where(p => !string.IsNullOrEmpty(p.InstallationNotes)).ToList();

    public List<ConfigurationUnitResultViewModel> ConfigurationUnitResults { get; private set; } = [];

    public List<ConfigurationUnitResultViewModel> TargetCloneResults { get; private set; } = [];

    public List<ConfigurationUnitResultViewModel> TargetInstallResults { get; private set; } = [];

    public List<ConfigurationUnitResultViewModel> TargetFailedResults { get; private set; } = [];

    public bool IsSettingUpATargetMachine => _orchestrator.IsSettingUpATargetMachine;

    /// <summary>
    /// Gets a value indicating whether a configuration file was used.
    /// </summary>
    public bool ShowConfigurationUnitResults => ConfigurationUnitResults.Count > 0;

    /// <summary>
    /// Gets a value indicating whether to show results for setting up a target machine.
    /// </summary>
    public bool ShowTargetMachineSetupResults => IsSettingUpATargetMachine && ShowConfigurationUnitResults;

    /// <summary>
    /// Gets a value indicating whether the configuration file flow was used.
    /// </summary>
    public bool ShowConfigurationFileResults => ShowConfigurationUnitResults && !IsSettingUpATargetMachine;

    public bool CompletedWithErrors => TargetFailedResults.Count > 0 || FailedTasks.Count > 0;

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

    [ObservableProperty]
    private string _summaryPageEnvironmentCreatingText;

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

    public void GoToDashboard()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("Dashboard"), Orchestrator.ActivityId);
        _host.GetService<INavigationService>().NavigateTo(KnownPageKeys.Dashboard);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToDashboard");
    }

    [RelayCommand]
    public void RedirectToNextPage()
    {
        if (WasCreateEnvironmentOperationStarted)
        {
            GoToEnvironmentsPage();
            return;
        }

        // Default behavior is to go to the dashboard
        GoToDashboard();
    }

    public void GoToEnvironmentsPage()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("Environments"), Orchestrator.ActivityId);
        _host.GetService<INavigationService>().NavigateTo(KnownPageKeys.Environments);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToEnvironments");
    }

    [RelayCommand]
    public void GoToDevHomeSettings()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("DevHomeSettings"), Orchestrator.ActivityId);
        _host.GetService<INavigationService>().NavigateTo(KnownPageKeys.Settings);
        _setupFlowViewModel.TerminateCurrentFlow("Summary_GoToSettings");
    }

    [RelayCommand]
    public void GoToForDevelopersSettingsPage()
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NavigateTo_Event", LogLevel.Critical, new NavigateFromSummaryEvent("WindowsDeveloperSettings"), Orchestrator.ActivityId);
        Task.Run(() => Launcher.LaunchUriAsync(new Uri("ms-settings:developers"))).Wait();
    }

    [ObservableProperty]
    private string _pageRedirectButtonText;

    [ObservableProperty]
    private string _pageHeaderText;

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

        _showRestartNeeded = Visibility.Collapsed;
        _appManagementInitializer = appManagementInitializer;
        _cloneRepoNextSteps = new();
        PageRedirectButtonText = StringResource.GetLocalized(StringResourceKey.SummaryPageOpenDashboard);
        PageHeaderText = StringResource.GetLocalized(StringResourceKey.SummaryPageHeader);

        IsNavigationBarVisible = true;
        IsStepPage = false;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        ConfigurationUnitResults = GetConfigurationUnitResults();

        TargetCloneResults = InitializeTargetResults(
            unitResult => unitResult.IsCloneRepoUnit && unitResult.IsSuccess && !unitResult.IsSkipped);

        TargetInstallResults = InitializeTargetResults(
            unitResult => !unitResult.IsCloneRepoUnit && unitResult.IsSuccess && !unitResult.IsSkipped);

        TargetFailedResults = InitializeTargetResults(
            unitResult => unitResult.IsError);

        IList<TaskInformation> failedTasks = new List<TaskInformation>();

        // Find the loading view model.
        foreach (var flowPage in _orchestrator.FlowPages)
        {
            if (flowPage is LoadingViewModel loadingViewModel)
            {
                failedTasks = loadingViewModel.FailedTasks;
            }
        }

        // Collect all next steps.
        var taskGroups = Orchestrator.TaskGroups;
        foreach (var taskGroup in taskGroups)
        {
            var setupTasks = taskGroup.SetupTasks;
            foreach (var setupTask in setupTasks)
            {
                if (setupTask.SummaryScreenInformation is not null &&
                    setupTask.SummaryScreenInformation.HasContent)
                {
                    switch (setupTask)
                    {
                        case CloneRepoTask:
                            var configResult = new CloneRepoSummaryInformationView();
                            configResult.DataContext = setupTask.SummaryScreenInformation;
                            _cloneRepoNextSteps.Add(configResult);
                            break;
                    }
                }
            }
        }

        // Send telemetry about the number of next steps tasks found broken down by their type.
        ReportSummaryTaskCounts(_cloneRepoNextSteps.Count);

        var statusSymbol = _host.GetService<IThemeSelectorService>().IsDarkTheme() ? DarkError : LightError;

        foreach (var failedTask in failedTasks)
        {
            var summaryMessageViewModel = new SummaryErrorMessageViewModel();
            summaryMessageViewModel.MessageToShow = failedTask.MessageToShow;
            summaryMessageViewModel.StatusSymbolIcon = statusSymbol;
            FailedTasks.Add(summaryMessageViewModel);
        }

        if (IsSettingUpATargetMachine)
        {
            if (TargetCloneResults.Count > 0)
            {
                var localizedHeader = (TargetCloneResults.Count == 1) ? StringResourceKey.SummaryPageOneRepositoryCloned : StringResourceKey.SummaryPageReposClonedCount;
                RepositoriesClonedText = StringResource.GetLocalized(localizedHeader);
            }

            if (TargetInstallResults.Count > 0)
            {
                var localizedHeader = (TargetInstallResults.Count == 1) ? StringResourceKey.SummaryPageOneApplicationInstalled : StringResourceKey.SummaryPageAppsDownloadedCount;
                ApplicationsClonedText = StringResource.GetLocalized(localizedHeader);
            }

            foreach (var targetFailedResult in TargetFailedResults)
            {
                targetFailedResult.StatusSymbolIcon = statusSymbol;
            }

            TargetFailedCountText = StringResource.GetLocalized(StringResourceKey.SummaryConfigurationErrorsCountText, TargetFailedResults.Count);

            if (FailedTasks.Count > 0)
            {
                // There is only one task group for setting up a target machine.
                FailedTasks[0].MessageToShow = StringResource.GetLocalized(StringResourceKey.SummaryPageTargetMachineFailedTaskText);
            }
        }

        // If any tasks failed in the loading screen, the user has to click on the "Next" button
        // If no tasks failed, the user is brought to the summary screen, no interaction required.
        if (failedTasks.Count != 0)
        {
            TelemetryFactory.Get<ITelemetry>().LogCritical("Summary_NavigatedTo_Event", false, Orchestrator.ActivityId);
        }

        if (WasCreateEnvironmentOperationStarted)
        {
            PageRedirectButtonText = StringResource.GetLocalized(StringResourceKey.SummaryPageRedirectToEnvironmentPageButton);
            PageHeaderText = StringResource.GetLocalized(StringResourceKey.SummaryPageHeaderForEnvironmentCreationText);
        }

        await ReloadCatalogsAsync();
    }

    /// <summary>
    /// Send telemetry about all next steps.
    /// </summary>
    private void ReportSummaryTaskCounts(int cloneRepoNextStepsCount)
    {
        TelemetryFactory.Get<ITelemetry>().Log("Summary_NextSteps_Event", LogLevel.Critical, new CloneRepoNextStepsEvent(cloneRepoNextStepsCount), Orchestrator.ActivityId);
    }

    private async Task ReloadCatalogsAsync()
    {
        // After installing packages, reconnect to catalogs to
        // reflect the latest changes when new Package COM objects are created
        _log.Information($"Checking if a new catalog connections should be established");
        if (_packageProvider.SelectedPackages.Any(package => package.CanInstall && package.InstallPackageTask.WasInstallSuccessful))
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
        if (IsSettingUpATargetMachine)
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

    private List<ConfigurationUnitResultViewModel> InitializeTargetResults(Func<ConfigurationUnitResultViewModel, bool> predicate)
    {
        List<ConfigurationUnitResultViewModel> unitResults = new();
        if (IsSettingUpATargetMachine)
        {
            unitResults.AddRange(ConfigurationUnitResults.Where(unitResult => predicate(unitResult)));
        }

        return unitResults;
    }
}
