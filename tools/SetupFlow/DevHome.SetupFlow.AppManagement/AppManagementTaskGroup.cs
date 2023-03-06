// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement;

public class AppManagementTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;

    public AppManagementTaskGroup(IHost host)
    {
        _host = host;
    }

    private readonly IList<InstallPackageTask> _installTasks = new List<InstallPackageTask>();

    public IEnumerable<ISetupTask> SetupTasks => _installTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<AppManagementViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<AppManagementReviewViewModel>(this);
}
