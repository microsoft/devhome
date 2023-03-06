// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.Review.ViewModels;

public partial class ReviewViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly SetupFlowOrchestrator _orchestrator;

    [ObservableProperty]
    private bool _isEulaAccepted = false;

    [ObservableProperty]
    private bool _isRebootRequired = false;

    [ObservableProperty]
    private bool _isRebootAccepted = false;

    [ObservableProperty]
    private IList<ReviewTabViewModelBase> _reviewTabs;

    [ObservableProperty]
    private ReviewTabViewModelBase _selectedReviewTab;

    public ReviewViewModel(ILogger logger, IStringResource stringResource, IHost host, SetupFlowOrchestrator orchestrator)
        : base(stringResource)
    {
        _logger = logger;
        _host = host;
        _orchestrator = orchestrator;

        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        CanGoToNextPage = false;
    }

    public async override void OnNavigateToPageAsync()
    {
        IsRebootRequired = _orchestrator.TaskGroups.Any(taskGroup => taskGroup.SetupTasks.Any(task => task.RequiresReboot));
        ReviewTabs = _orchestrator.TaskGroups.Select(taskGroup => taskGroup.GetReviewTabViewModel()).ToList();
        SelectedReviewTab = ReviewTabs.FirstOrDefault();
        await Task.CompletedTask;
    }

    partial void OnIsEulaAcceptedChanged(bool value) => UpdateCanGoToNextPage();

    partial void OnIsRebootAcceptedChanged(bool value) => UpdateCanGoToNextPage();

    public void UpdateCanGoToNextPage()
    {
        CanGoToNextPage =
            IsEulaAccepted &&
            (!IsRebootRequired || IsRebootAccepted) &&
            _orchestrator.TaskGroups.Any(g => g.SetupTasks.Any());
        _orchestrator.NotifyNavigationCanExecuteChanged();
    }
}
