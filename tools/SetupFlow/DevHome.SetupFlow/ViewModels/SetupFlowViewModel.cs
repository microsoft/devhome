// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Windows.Storage;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupFlowViewModel : ObservableObject
{
    private readonly IHost _host;
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly PackageProvider _packageProvider;

    public SetupFlowOrchestrator Orchestrator { get; }

    public SetupFlowViewModel(
        IHost host,
        SetupFlowOrchestrator orchestrator,
        PackageProvider packageProvider)
    {
        _host = host;
        Orchestrator = orchestrator;
        _packageProvider = packageProvider;

        // Set initial view
        _mainPageViewModel = _host.GetService<MainPageViewModel>();
        Orchestrator.FlowPages = new List<SetupPageViewModelBase>
        {
            _mainPageViewModel,
        };

        _mainPageViewModel.StartSetupFlow += (object sender, (string, IList<ISetupTaskGroup>) args) =>
        {
            var flowTitle = args.Item1;
            var taskGroups = args.Item2;

            // Don't reset the title when on an empty string; may have set it earlier to what we want
            if (!string.IsNullOrEmpty(flowTitle))
            {
                Orchestrator.FlowTitle = flowTitle;
            }

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
    private void Cancel()
    {
        var currentPage = Orchestrator.CurrentPageViewModel.GetType().Name;
        TerminateCurrentFlow($"CancelButton_{currentPage}");
    }

    public void TerminateCurrentFlow(string callerNameForTelemetry)
    {
        // Report this before touching the pages so the current Activity ID can be obtained.
        Log.Logger?.ReportInfo(Log.Component.Orchestrator, $"Terminating Setup flow by caller [{callerNameForTelemetry}]. ActivityId={Orchestrator.ActivityId}");
        TelemetryFactory.Get<ITelemetry>().Log("SetupFlow_Termination", LogLevel.Critical, new EndFlowEvent(callerNameForTelemetry), relatedActivityId: Orchestrator.ActivityId);

        Orchestrator.ReleaseRemoteOperationObject();
        _host.GetService<IDevDriveManager>().RemoveAllDevDrives();
        _packageProvider.Clear();

        Orchestrator.FlowPages = new List<SetupPageViewModelBase> { _mainPageViewModel };
    }

    public async Task StartFileActivationFlow(StorageFile file)
    {
        // Cancel whatever existing operations exist in setup flow
        Orchestrator.FlowPages = new List<SetupPageViewModelBase> { _mainPageViewModel };

        await _mainPageViewModel.StartFileActivationAsync(file);
    }
}
