// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.Summary.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;

    public SummaryViewModel(ILogger logger, SetupFlowStringResource stringResource)
        : base(stringResource)
    {
        _logger = logger;
        IsNavigationBarVisible = false;
        IsStepPage = false;
    }
}
