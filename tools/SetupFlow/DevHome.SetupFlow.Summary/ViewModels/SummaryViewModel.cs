// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.Summary.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly SetupFlowOrchestrator _orchestrator;

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        ILogger logger)
        : base(stringResource, orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;

        IsNavigationBarVisible = false;
        IsStepPage = false;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        // Disposing of this object allows signals the background process to terminate.
        _orchestrator.RemoteElevatedFactory?.Dispose();
        _orchestrator.RemoteElevatedFactory = null;
        await Task.CompletedTask;
    }
}
