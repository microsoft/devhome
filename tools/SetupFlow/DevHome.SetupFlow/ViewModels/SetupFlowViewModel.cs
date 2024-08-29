// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Environments.Models;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
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
    private readonly ISetupFlowStringResource _stringResource;
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly PackageProvider _packageProvider;

    private readonly string _creationFlowNavigationParameter = "StartCreationFlow";
    private readonly string _configurationFlowNavigationParameter = "StartConfigurationFlow";
    private readonly string _quickstartNavigationParameter = "StartQuickstartPlayground";

    public SetupFlowOrchestrator Orchestrator { get; }

    public event EventHandler EndSetupFlow = (s, e) => { };

    public SetupFlowViewModel(
        IHost host,
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        PackageProvider packageProvider)
    {
        _host = host;
        _stringResource = stringResource;
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

    public void StartCreationFlow(string originPage)
    {
        Orchestrator.FlowPages = [_mainPageViewModel];

        // This method is only called when the user clicks a button that redirects them to 'Create Environment' flow in the setup flow.
        _mainPageViewModel.StartCreateEnvironmentWithTelemetry(string.Empty, _creationFlowNavigationParameter, originPage);
    }

    public void StartSetupFlow(string originPage, ComputeSystemReviewItem item)
    {
        Orchestrator.FlowPages = [_mainPageViewModel];

        // This method is only called when the user clicks a button that redirects them to 'Setup' flow in the Environments page.
        _mainPageViewModel.StartSetupForTargetEnvironmentWithTelemetry(string.Empty, _configurationFlowNavigationParameter, originPage, item);
    }

    public void OnNavigatedTo(NavigationEventArgs args)
    {
        // The setup flow isn't set up to support using the navigation service to navigate to specific
        // pages. Instead we need to navigate to the main page and then start the creation flow template manually.
        var parameter = args.Parameter?.ToString();

        if ((!string.IsNullOrEmpty(parameter)) &&
            parameter.Contains(_creationFlowNavigationParameter, StringComparison.OrdinalIgnoreCase))
        {
            // We expect that when navigating from anywhere in Dev Home to the create environment page
            // that the arg.Parameter variable be semicolon delimited string with the first value being 'StartCreationFlow'
            // and the second value being the page name that redirection came from for telemetry purposes.
            var parameters = parameter.Split(';');
            Cancel();
            StartCreationFlow(originPage: parameters[1]);
        }
        else if ((!string.IsNullOrEmpty(parameter)) &&
            parameter.Contains(_quickstartNavigationParameter, StringComparison.OrdinalIgnoreCase))
        {
            Cancel();
            Orchestrator.FlowPages = [_mainPageViewModel];
            var flowTitle = _stringResource.GetLocalized("MainPage_QuickstartPlayground/Header");
            _mainPageViewModel.StartQuickstart(flowTitle);
        }
        else if (args.Parameter is object[] configObjs && configObjs.Length == 3)
        {
            if (configObjs[0] is string configObj && configObj.Equals(_configurationFlowNavigationParameter, StringComparison.OrdinalIgnoreCase))
            {
                // We expect that when navigating from anywhere in Dev Home to the create environment page
                // that the arg.Parameter variable be an object array with the the first value being 'StartCreationFlow',
                // the second value being the page name that redirection came from for telemetry purposes, and
                // the third value being the ComputeSystemReviewItem to setup.
                Cancel();
                StartSetupFlow(originPage: configObjs[1] as string, item: configObjs[2] as ComputeSystemReviewItem);
            }
        }
    }

    public void StartAppManagementFlow(string query = null)
    {
        Orchestrator.FlowPages = [_mainPageViewModel];
        _mainPageViewModel.StartAppManagementFlow(query);
    }
}
