// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    }

    public IEnumerable<ISetupTask> SetupTasks => _packageProvider.SelectedPackages
        .Where(sp => sp.CanInstall)
        .Select(sp => sp.InstallPackageTask);

    public IEnumerable<ISetupTask> DSCTasks => _packageProvider.SelectedPackages
        .Select(sp => sp.InstallPackageTask);

    public SetupPageViewModelBase GetSetupPageViewModel() => _appManagementViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _appManagementReviewViewModel;

    public void HandleSearchQuery(string query)
    {
        _appManagementViewModel.PerformSearch(query);
    }
}
