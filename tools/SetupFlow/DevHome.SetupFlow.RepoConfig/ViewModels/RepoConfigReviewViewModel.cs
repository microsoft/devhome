// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

public partial class RepoConfigReviewViewModel : ReviewTabViewModelBase
{
    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly RepoConfigTaskGroup _taskGroup;

    public RepoConfigReviewViewModel(ILogger logger, ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        _taskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
