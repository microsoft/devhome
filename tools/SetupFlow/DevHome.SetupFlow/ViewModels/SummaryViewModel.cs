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
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly SetupFlowOrchestrator _orchestrator;
    private readonly IHost _host;

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    // TODO: refactor setup flow so CloneRepoTask can be used without having to
    // add the app management project.
    public ObservableCollection<KeyValuePair<string, string>> RepositoriesCloned
    {
        get
        {
            var repositoriesCloned = new ObservableCollection<KeyValuePair<string, string>>();
            var taskGroup = _host.GetService<SetupFlowOrchestrator>().TaskGroups;
            var group = taskGroup.SingleOrDefault(x => x.GetType() == typeof(RepoConfigTaskGroup));
            if (group is RepoConfigTaskGroup repoTaskGroup)
            {
                foreach (var task in repoTaskGroup.SetupTasks)
                {
                    if (task is CloneRepoTask repoTask && repoTask.WasCloningSuccessful)
                    {
                        repositoriesCloned.Add(
                            new KeyValuePair<string, string>(GetFontIconForProvider(repoTask.ProviderName), repoTask.RepositoryName));
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

    public IList<ConfigurationFileHelper.UnitResult> ConfigurationUnits
    {
        get
        {
            List<ConfigurationFileHelper.UnitResult> test = new ();
            var repositoriesCloned = new ObservableCollection<KeyValuePair<string, string>>();
            var taskGroup = _host.GetService<SetupFlowOrchestrator>().TaskGroups;
            var group = taskGroup.SingleOrDefault(x => x.GetType() == typeof(ConfigurationFileTaskGroup));
            if (group is ConfigurationFileTaskGroup configTaskGroup)
            {
                foreach (var task in configTaskGroup.SetupTasks)
                {
                    if (task is ConfigureTask configTask)
                    {
                        test.AddRange(configTask.Result.UnitResults);
                    }
                }
            }

            return test;
        }
    }

    [RelayCommand]
    public void OpenDashboard()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void RemoveRestartGrid()
    {
        _showRestartNeeded = Visibility.Collapsed;
    }

    [RelayCommand]
    public void GoBackToMainPage()
    {
        var things = _host.GetService<SetupFlowViewModel>();
        things.Cancel();
    }

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _orchestrator = orchestrator;
        _host = host;

        IsNavigationBarVisible = false;
        IsStepPage = false;

        _showRestartNeeded = Visibility.Collapsed;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        _orchestrator.ReleaseRemoteFactory();
        await Task.CompletedTask;
    }

    // This can possibly be moved to a more central location
    public string GetFontIconForProvider(string providerName) => providerName switch
    {
        // Puzzle piece icon
        _ => "\uEA86",
    };
}
