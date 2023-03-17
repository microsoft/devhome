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
    private readonly IWindowsPackageManager _wpm;

    public AppManagementTaskGroup(IHost host, IWindowsPackageManager wpm, PackageProvider packageProvider)
    {
        _host = host;
        _packageProvider = packageProvider;
        _wpm = wpm;

        _packageProvider.Clear();
    }

    public IEnumerable<ISetupTask> SetupTasks => _packageProvider.SelectedPackages.Select(sp => sp.Package.CreateInstallTask(_wpm));

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<AppManagementViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<AppManagementReviewViewModel>(this);
}
