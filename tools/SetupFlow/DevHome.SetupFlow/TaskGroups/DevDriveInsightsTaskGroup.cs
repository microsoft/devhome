// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.TaskGroups;

public class DevDriveInsightsTaskGroup : ISetupTaskGroup
{
    private readonly DevDriveInsightsViewModel _devDriveInsightsViewModel;
    private readonly DevDriveInsightsReviewViewModel _devDriveInsightsReviewViewModel;

    private readonly ConfigureTargetTask _devDriveInsightsTaskGroup;

    public DevDriveInsightsTaskGroup(
        DevDriveInsightsViewModel devDriveInsightsViewModel,
        DevDriveInsightsReviewViewModel devDriveInsightsReviewViewModel,
        ISetupFlowStringResource stringResource,
        IComputeSystemManager computeSystemManager,
        ConfigurationFileBuilder configurationFileBuilder,
        SetupFlowOrchestrator setupFlowOrchestrator,
        IThemeSelectorService themeSelectorService)
    {
        _devDriveInsightsViewModel = devDriveInsightsViewModel;
        _devDriveInsightsReviewViewModel = devDriveInsightsReviewViewModel;

        _devDriveInsightsTaskGroup = new ConfigureTargetTask(
            stringResource,
            computeSystemManager,
            configurationFileBuilder,
            setupFlowOrchestrator,
            themeSelectorService);
    }

    /// <summary>
    /// Gets the task corresponding to the configuration file to apply
    /// </summary>
    /// <remarks>At most one configuration file can be applied at a time</remarks>
    public ConfigureTargetTask ConfigureTask => _devDriveInsightsTaskGroup;

    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>() { _devDriveInsightsTaskGroup };

    public SetupPageViewModelBase GetSetupPageViewModel() => _devDriveInsightsViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _devDriveInsightsReviewViewModel;
}
