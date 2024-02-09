// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.ViewModels;

public class SetupTargetReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    public override bool HasItems => _computeSystemManager.ComputeSystemSetupItem != null;

    public ComputeSystemReviewItem ComputeSystemSetupItem => _computeSystemManager.ComputeSystemSetupItem;

    public SetupTargetReviewViewModel(ISetupFlowStringResource stringResource, IComputeSystemManager computeSystemManager)
    {
        _stringResource = stringResource;
        TabTitle = stringResource.GetLocalized(StringResourceKey.SetupTargetPageTitle);

        _computeSystemManager = computeSystemManager;
    }
}
