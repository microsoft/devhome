// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.TaskGroups;

public class SetupTargetTaskGroup : ISetupTaskGroup
{
    private readonly SetupTargetViewModel _setupTargetViewModel;
    private readonly SetupTargetReviewViewModel _setupTargetReviewViewModel;

    public SetupTargetTaskGroup(
        SetupTargetViewModel setupTargetViewModel,
        SetupTargetReviewViewModel setupTargetReviewViewModel)
    {
        _setupTargetViewModel = setupTargetViewModel;
        _setupTargetReviewViewModel = setupTargetReviewViewModel;
    }

    // TODO: Update this to provide specific tasks for the setup target task group.
    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>();

    public SetupPageViewModelBase GetSetupPageViewModel() => _setupTargetViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _setupTargetReviewViewModel;
}
