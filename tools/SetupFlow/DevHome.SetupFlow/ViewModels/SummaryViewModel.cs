// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Dashboard.ViewModels;
using DevHome.Settings.ViewModels;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Selectors;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Views;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly SetupFlowOrchestrator _orchestrator;
    private readonly IHost _host;
    private readonly Lazy<IList<ConfigurationUnitResultViewModel>> _configurationUnitResults;
    private readonly ConfigurationUnitResultViewModelFactory _configurationUnitResultViewModelFactory;
    private readonly IWindowsPackageManager _wpm;

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    // TODO: refactor setup flow so CloneRepoTask can be used without having to
    // add the app management project.
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

            return repositoriesCloned;
        }
    }

    // TODO: refactor setup flow so PackageViewModel and PackageProvider can be used without having to
    // add the app management project.
    public ObservableCollection<PackageViewModel> AppsDownloaded
    {
        get
        {
            var packagesInstalled = new ObservableCollection<PackageViewModel>();
            var packageProvider = _host.GetService<PackageProvider>();
            var packages = packageProvider.SelectedPackages.Where(sp => sp.InstallPackageTask.WasInstallSuccessful == true);
            foreach (var package in packages)
            {
                packagesInstalled.Add(package);
            }

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

    [RelayCommand]
    public void RemoveRestartGrid()
    {
        _showRestartNeeded = Visibility.Collapsed;
    }

    [RelayCommand]
    public void GoToMainPage()
    {
        var setupFlowViewModel = _host.GetService<SetupFlowViewModel>();
        setupFlowViewModel.Cancel();
    }

    [RelayCommand]
    public void GoToDashboard()
    {
        _host.GetService<INavigationService>().NavigateTo(typeof(DashboardViewModel).FullName);
    }

    [RelayCommand]
    public void GoToDevHomeSettings()
    {
        _host.GetService<INavigationService>().NavigateTo(typeof(SettingsViewModel).FullName);
    }

    [RelayCommand]
    public void GoToForDevelopersSettingsPage()
    {
        Task.Run(() => Launcher.LaunchUriAsync(new Uri("ms-settings:developers"))).Wait();
    }

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host,
        ConfigurationUnitResultViewModelFactory configurationUnitResultViewModelFactory,
        IWindowsPackageManager wpm)
        : base(stringResource, orchestrator)
    {
        _orchestrator = orchestrator;
        _host = host;
        _configurationUnitResultViewModelFactory = configurationUnitResultViewModelFactory;
        _wpm = wpm;

        IsNavigationBarVisible = false;
        IsStepPage = false;
        _configurationUnitResults = new (GetConfigurationUnitResults);

        _showRestartNeeded = Visibility.Collapsed;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        _orchestrator.ReleaseRemoteFactory();
        await ReloadCatalogsAsync();
    }

    private async Task ReloadCatalogsAsync()
    {
        var packageProvider = _host.GetService<PackageProvider>();
        var catalogProvider = _host.GetService<CatalogProvider>();

        // After installing packages, we should reconnect to catalogs to
        // reflect the latest changes when new Package COM objects are created
        Log.Logger?.ReportInfo(Log.Component.Summary, $"Checking if a new catalog connections should be established");
        if (packageProvider.SelectedPackages.Any(package => package.InstallPackageTask.WasInstallSuccessful))
        {
            await Task.Run(async () =>
            {
                Log.Logger?.ReportInfo(Log.Component.Summary, $"Creating a new catalog connections");
                await _wpm.ConnectToAllCatalogsAsync(force: true);

                Log.Logger?.ReportInfo(Log.Component.Summary, $"Reloading catalogs from all data sources");
                catalogProvider.Clear();
                await foreach (var dataSourceCatalogs in catalogProvider.LoadCatalogsAsync())
                {
                    Log.Logger?.ReportInfo(Log.Component.Summary, $"Reloaded {dataSourceCatalogs.Count} catalog(s)");
                }
            });
        }
    }

    /// <summary>
    /// Get the list of configuratoin unit restults for an applied
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
