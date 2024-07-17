// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;

namespace DevHome.SetupFlow.TaskGroups;

public class EnvironmentCreationOptionsTaskGroup : ISetupTaskGroup
{
    private readonly ISetupFlowStringResource _setupFlowStringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly EnvironmentCreationOptionsViewModel _environmentCreationOptionsViewModel;

    private readonly CreateEnvironmentReviewViewModel _createEnvironmentReviewViewModel;

    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public CreateEnvironmentTask CreateEnvironmentTask { get; private set; }

    public EnvironmentCreationOptionsTaskGroup(
        SetupFlowViewModel setupFlowViewModel,
        IComputeSystemManager computeSystemManager,
        ISetupFlowStringResource setupFlowStringResource,
        EnvironmentCreationOptionsViewModel environmentCreationOptionsViewModel,
        CreateEnvironmentReviewViewModel createEnvironmentReviewViewModel)
    {
        _environmentCreationOptionsViewModel = environmentCreationOptionsViewModel;
        _createEnvironmentReviewViewModel = createEnvironmentReviewViewModel;
        _setupFlowStringResource = setupFlowStringResource;
        _computeSystemManager = computeSystemManager;
        CreateEnvironmentTask = new CreateEnvironmentTask(_computeSystemManager, _setupFlowStringResource, setupFlowViewModel);
    }

    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>() { CreateEnvironmentTask };

    public IEnumerable<ISetupTask> DSCTasks => new List<ISetupTask>();

    public SetupPageViewModelBase GetSetupPageViewModel() => _environmentCreationOptionsViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _createEnvironmentReviewViewModel;
}
