// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.ElevatedComponent;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class ReviewViewModel : SetupPageViewModelBase
{
    private readonly IHost _host;
    private readonly SetupFlowOrchestrator _orchestrator;

    [ObservableProperty]
    private IList<ReviewTabViewModelBase> _reviewTabs;

    [ObservableProperty]
    private ReviewTabViewModelBase _selectedReviewTab;

    public bool HasApplicationsToInstall => Orchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Any() == true;

    public bool HasMSStoreApplicationsToInstall
    {
        get
        {
            var hasMSStoreApps = Orchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Any(task =>
            {
                var installTask = task as InstallPackageTask;
                return installTask?.IsFromMSStore == true;
            });

            return hasMSStoreApps == true;
        }
    }

    public bool HasTasksToSetUp => Orchestrator.TaskGroups.Any(g => g.SetupTasks.Any());

    public ReviewViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _host = host;
        _orchestrator = orchestrator;

        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReviewPageTitle);
        CanGoToNextPage = false;
    }

    protected async override Task OnEachNavigateToAsync()
    {
        NextPageButtonToolTipText = HasTasksToSetUp ? null : StringResource.GetLocalized(StringResourceKey.ReviewNothingToSetUpToolTip);
        await Task.CompletedTask;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        ReviewTabs = _orchestrator.TaskGroups.Select(taskGroup => taskGroup.GetReviewTabViewModel()).ToList();
        SelectedReviewTab = ReviewTabs.FirstOrDefault();
        await Task.CompletedTask;
    }

    protected async override Task OnFirstNavigateFromAsync()
    {
        var isAdminRequired = _orchestrator.TaskGroups.Any(taskGroup => taskGroup.SetupTasks.Any(task => task.RequiresAdmin));
        if (isAdminRequired)
        {
            _orchestrator.RemoteElevatedFactory = await IPCSetup.CreateOutOfProcessObjectAsync<IElevatedComponentFactory>();
        }

        await Task.CompletedTask;
    }

    public void UpdateCanGoToNextPage()
    {
        CanGoToNextPage = HasTasksToSetUp;
        _orchestrator.NotifyNavigationCanExecuteChanged();
    }
}
