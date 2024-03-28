// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Environments.Services;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.TaskGroups;

public class SetupTargetTaskGroup : ISetupTaskGroup
{
    private readonly SetupTargetViewModel _setupTargetViewModel;
    private readonly SetupTargetReviewViewModel _setupTargetReviewViewModel;

    private readonly ConfigureTargetTask _setupTargetTaskGroup;

    public SetupTargetTaskGroup(
        SetupTargetViewModel setupTargetViewModel,
        SetupTargetReviewViewModel setupTargetReviewViewModel,
        ISetupFlowStringResource stringResource,
        IComputeSystemManager computeSystemManager,
        ConfigurationFileBuilder configurationFileBuilder,
        SetupFlowOrchestrator setupFlowOrchestrator)
    {
        _setupTargetViewModel = setupTargetViewModel;
        _setupTargetReviewViewModel = setupTargetReviewViewModel;

        _setupTargetTaskGroup = new ConfigureTargetTask(
            stringResource,
            computeSystemManager,
            configurationFileBuilder,
            setupFlowOrchestrator);
    }

    /// <summary>
    /// Gets the task corresponding to the configuration file to apply
    /// </summary>
    /// <remarks>At most one configuration file can be applied at a time</remarks>
    public ConfigureTargetTask ConfigureTask => _setupTargetTaskGroup;

    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>() { _setupTargetTaskGroup };

    public SetupPageViewModelBase GetSetupPageViewModel() => _setupTargetViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _setupTargetReviewViewModel;
}
