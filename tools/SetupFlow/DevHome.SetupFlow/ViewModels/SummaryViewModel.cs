// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
using DevHome.Dashboard.ViewModels;
using DevHome.Settings.ViewModels;
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

            packageProvider.Clear();
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

    [RelayCommand]
    public void GoToLearnMorePage()
    {
        var browserProcessInfo = new ProcessStartInfo();
        browserProcessInfo.UseShellExecute = true;
        browserProcessInfo.FileName = "https://learn.microsoft.com/windows/";

        Process.Start(browserProcessInfo);
    }

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host,
        ConfigurationUnitResultViewModelFactory configurationUnitResultViewModelFactory)
        : base(stringResource, orchestrator)
    {
        _orchestrator = orchestrator;
        _host = host;
        _configurationUnitResultViewModelFactory = configurationUnitResultViewModelFactory;

        IsNavigationBarVisible = false;
        IsStepPage = false;
        _configurationUnitResults = new (GetConfigurationUnitResults);

        _showRestartNeeded = Visibility.Collapsed;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        _orchestrator.ReleaseRemoteFactory();
        await Task.CompletedTask;
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
