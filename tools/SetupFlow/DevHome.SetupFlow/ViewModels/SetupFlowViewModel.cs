// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupFlowViewModel : ObservableObject
{
    private readonly IHost _host;
    private readonly MainPageViewModel _mainPageViewModel;

    public SetupFlowOrchestrator Orchestrator { get; }

    public SetupFlowViewModel(IHost host, SetupFlowOrchestrator orchestrator)
    {
        _host = host;
        Orchestrator = orchestrator;

        // Set initial view
        _mainPageViewModel = _host.GetService<MainPageViewModel>();
        Orchestrator.FlowPages = new List<SetupPageViewModelBase>
        {
            _mainPageViewModel,
        };

        _mainPageViewModel.StartSetupFlow += (object sender, IList<ISetupTaskGroup> taskGroups) =>
        {
            Orchestrator.TaskGroups = taskGroups;
            SetFlowPagesFromCurrentTaskGroups();
        };
    }

    public void SetFlowPagesFromCurrentTaskGroups()
    {
        _host.GetService<IDevDriveManager>().RemoveAllDevDrives();
        List<SetupPageViewModelBase> flowPages = new ();
        flowPages.AddRange(Orchestrator.TaskGroups.Select(flow => flow.GetSetupPageViewModel()).Where(page => page is not null));

        // Check if the review page should be added as a step
        if (Orchestrator.TaskGroups.Any(flow => flow.GetReviewTabViewModel() != null))
        {
            flowPages.Add(_host.GetService<ReviewViewModel>());
        }
        else
        {
            Log.Logger?.ReportInfo(Log.Component.Orchestrator, "Review page will be skipped for this flow");
        }

        // The Loading page can advance to the next page
        // without user interaction once it is complete
        var loadingPageViewModel = _host.GetService<LoadingViewModel>();
        flowPages.Add(loadingPageViewModel);

        loadingPageViewModel.ExecutionFinished += async (object _, EventArgs _) =>
        {
            await Orchestrator.GoToNextPage();
        };

        flowPages.Add(_host.GetService<SummaryViewModel>());

        Orchestrator.FlowPages = flowPages;
    }

    [RelayCommand]
    public void Cancel()
    {
        Log.Logger?.ReportInfo(Log.Component.Orchestrator, "Cancelling flow");
        Orchestrator.ReleaseRemoteFactory();
        _host.GetService<IDevDriveManager>().RemoveAllDevDrives();
        Orchestrator.FlowPages = new List<SetupPageViewModelBase> { _mainPageViewModel };
    }
}
