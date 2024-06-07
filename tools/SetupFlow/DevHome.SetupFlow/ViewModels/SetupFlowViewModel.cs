// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.Environments;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Windows.Storage;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupFlowViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SetupFlowViewModel));
    private readonly IHost _host;
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly PackageProvider _packageProvider;

    private readonly string _creationFlowNavigationParameter = "StartCreationFlow";

    public SetupFlowOrchestrator Orchestrator { get; }

    public event EventHandler EndSetupFlow = (s, e) => { };

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
        List<SetupPageViewModelBase> flowPages = new();
        flowPages.AddRange(Orchestrator.TaskGroups.Select(flow => flow.GetSetupPageViewModel()).Where(page => page is not null));

        // Check if the review page should be added as a step
        if (Orchestrator.TaskGroups.Any(flow => flow.GetReviewTabViewModel() != null))
        {
            flowPages.Add(_host.GetService<ReviewViewModel>());
        }
        else
        {
            _log.Information("Review page will be skipped for this flow");
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
        _log.Information($"Terminating Setup flow by caller [{callerNameForTelemetry}]. ActivityId={Orchestrator.ActivityId}");
        TelemetryFactory.Get<ITelemetry>().Log("SetupFlow_Termination", LogLevel.Critical, new EndFlowEvent(callerNameForTelemetry), relatedActivityId: Orchestrator.ActivityId);

        Orchestrator.ReleaseRemoteOperationObject();
        _host.GetService<IDevDriveManager>().RemoveAllDevDrives();
        _packageProvider.Clear();
        EndSetupFlow(null, EventArgs.Empty);

        Orchestrator.FlowPages = new List<SetupPageViewModelBase> { _mainPageViewModel };
    }

    public async Task StartFileActivationFlowAsync(StorageFile file)
    {
        Orchestrator.FlowPages = [_mainPageViewModel];
        await _mainPageViewModel.StartConfigurationFileAsync(file);
    }

    public void StartCreationFlowAsync(string originPage)
    {
        Orchestrator.FlowPages = [_mainPageViewModel];

        // this method is only called when the user clicks a button that redirects them to 'Create Environment' flow in the setup flow.
        TelemetryFactory.Get<ITelemetry>().Log(
            "Create_Environment_button_Clicked",
            LogLevel.Critical,
            new EnvironmentRedirectionUserEvent(navigationAction: _creationFlowNavigationParameter, originPage),
            relatedActivityId: Orchestrator.ActivityId);

        _mainPageViewModel.StartCreateEnvironment(string.Empty);
    }

    public void OnNavigatedTo(NavigationEventArgs args)
    {
        // The setup flow isn't setup to support using the navigation service to navigate to specific
        // pages. Instead we need to navigate to the main page and then start the creation flow template manually.
        var parameters = $"{args.Parameter}".Split(';');

        if ((parameters.Length == 2) &&
            _creationFlowNavigationParameter.Equals(parameters[0], StringComparison.OrdinalIgnoreCase) &&
            Orchestrator.CurrentSetupFlowKind != SetupFlowKind.CreateEnvironment)
        {
            Cancel();
            StartCreationFlowAsync(originPage: parameters[1]);
        }
    }

    public void StartAppManagementFlow(string query = null)
    {
        Orchestrator.FlowPages = [_mainPageViewModel];
        _mainPageViewModel.StartAppManagementFlow(query);
    }
}
