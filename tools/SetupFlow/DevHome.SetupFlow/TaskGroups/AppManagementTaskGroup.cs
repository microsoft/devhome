// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.TaskGroups;

public class AppManagementTaskGroup : ISetupTaskGroup
{
    private readonly PackageProvider _packageProvider;
    private readonly AppManagementViewModel _appManagementViewModel;
    private readonly AppManagementReviewViewModel _appManagementReviewViewModel;

    public AppManagementTaskGroup(
        PackageProvider packageProvider,
        AppManagementViewModel appManagementViewModel,
        AppManagementReviewViewModel appManagementReviewViewModel)
    {
        _packageProvider = packageProvider;
        _appManagementViewModel = appManagementViewModel;
        _appManagementReviewViewModel = appManagementReviewViewModel;

        // TODO Convert the package provider to a scoped instance, to avoid
        // clearing it here. This requires refactoring and adding scopes when
        // creating task groups in the main page.
        _packageProvider.Clear();
    }

    public IEnumerable<ISetupTask> SetupTasks => _packageProvider.SelectedPackages.Select(sp => sp.InstallPackageTask);

    public SetupPageViewModelBase GetSetupPageViewModel() => _appManagementViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _appManagementReviewViewModel;
}
