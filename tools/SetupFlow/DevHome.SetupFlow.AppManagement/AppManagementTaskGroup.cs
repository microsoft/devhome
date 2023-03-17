// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
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
        _packageProvider.Clear();
    }

    private readonly IList<InstallPackageTask> _installTasks = new List<InstallPackageTask>();

    public IEnumerable<ISetupTask> SetupTasks => _installTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<AppManagementViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<AppManagementReviewViewModel>(this);
}
