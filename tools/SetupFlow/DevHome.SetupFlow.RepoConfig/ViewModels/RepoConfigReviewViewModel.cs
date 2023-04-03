// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

public partial class RepoConfigReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly RepoConfigTaskGroup _taskGroup;

    public RepoConfigReviewViewModel(ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _stringResource = stringResource;
        _taskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
