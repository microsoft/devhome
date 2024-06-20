// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.TaskGroups;

public class DeveloperQuickstartTaskGroup : ISetupTaskGroup
{
    private readonly QuickstartPlaygroundViewModel _viewModel;

    public DeveloperQuickstartTaskGroup(QuickstartPlaygroundViewModel quickstartPlaygroundViewModel)
    {
        _viewModel = quickstartPlaygroundViewModel;
    }

    public IEnumerable<ISetupTask> SetupTasks => throw new NotImplementedException();

    public IEnumerable<ISetupTask> DSCTasks => throw new NotImplementedException();

    public ReviewTabViewModelBase GetReviewTabViewModel()
    {
        // Developer quickstart does not have a review tab
        return null;
    }

    public SetupPageViewModelBase GetSetupPageViewModel() => _viewModel;
}
