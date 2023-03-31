// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.Loading.ViewModels;
using DevHome.SetupFlow.MainPage.ViewModels;
using DevHome.SetupFlow.Review.ViewModels;
using DevHome.SetupFlow.Summary.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupFlowViewModel : ObservableObject
{
    private readonly IHost _host;
    private readonly ILogger _logger;
    private readonly MainPageViewModel _mainPageViewModel;

    public SetupFlowOrchestrator Orchestrator { get; }

    public SetupFlowViewModel(IHost host, ILogger logger, SetupFlowOrchestrator orchestrator)
    {
        _host = host;
        _logger = logger;
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
        flowPages.Add(_host.GetService<ReviewViewModel>());

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
        Orchestrator.ReleaseRemoteFactory();
        _host.GetService<IDevDriveManager>().RemoveAllDevDrives();
        Orchestrator.FlowPages = new List<SetupPageViewModelBase> { _mainPageViewModel };
    }
}
