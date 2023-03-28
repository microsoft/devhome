// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement;

public class AppManagementTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;
    private readonly PackageProvider _packageProvider;

    public AppManagementTaskGroup(IHost host, PackageProvider packageProvider)
    {
        _host = host;
        _packageProvider = packageProvider;

        // TODO Convert the package provider to a scoped instance, to avoid
        // clearing it here. This requires refactoring and adding scopes when
        // creating task groups in the main page.
        _packageProvider.Clear();
    }

    public IEnumerable<ISetupTask> SetupTasks => _packageProvider.SelectedPackages.Select(sp => sp.InstallPackageTask);

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<AppManagementViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<AppManagementReviewViewModel>(this);
}
