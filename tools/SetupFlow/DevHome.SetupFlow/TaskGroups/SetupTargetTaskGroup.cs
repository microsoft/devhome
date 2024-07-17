// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Dispatching;

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
        SetupFlowOrchestrator setupFlowOrchestrator,
        DispatcherQueue dispatcherQueue)
    {
        _setupTargetViewModel = setupTargetViewModel;
        _setupTargetReviewViewModel = setupTargetReviewViewModel;

        _setupTargetTaskGroup = new ConfigureTargetTask(
            stringResource,
            computeSystemManager,
            configurationFileBuilder,
            setupFlowOrchestrator,
            dispatcherQueue);
    }

    /// <summary>
    /// Gets the task corresponding to the configuration file to apply
    /// </summary>
    /// <remarks>At most one configuration file can be applied at a time</remarks>
    public ConfigureTargetTask ConfigureTask => _setupTargetTaskGroup;

    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>() { _setupTargetTaskGroup };

    public IEnumerable<ISetupTask> DSCTasks => SetupTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _setupTargetViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _setupTargetReviewViewModel;
}
