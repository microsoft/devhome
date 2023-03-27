// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.Summary.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        ILogger logger)
        : base(stringResource, orchestrator)
    {
        _logger = logger;
        IsNavigationBarVisible = false;
        IsStepPage = false;
    }
}
