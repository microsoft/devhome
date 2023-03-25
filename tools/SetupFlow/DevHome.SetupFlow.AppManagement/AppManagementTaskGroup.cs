// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement;

public class AppManagementTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;
    private readonly PackageProvider _packageProvider;
    private readonly IWindowsPackageManager _wpm;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly ILogger _logger;

    public AppManagementTaskGroup(
        IHost host,
        IWindowsPackageManager wpm,
        PackageProvider packageProvider,
        ISetupFlowStringResource stringResource,
        ILogger logger)
    {
        _host = host;
        _packageProvider = packageProvider;
        _wpm = wpm;
        _stringResource = stringResource;
        _logger = logger;

        // TODO Convert the package provider to a scoped instance, to avoid
        // clearing it here. This requires refactoring and adding scopes when
        // creating task groups.
        // Clear package provider cache
        _packageProvider.Clear();
    }

    public IEnumerable<ISetupTask> SetupTasks => _packageProvider.SelectedPackages
        .Select(sp => sp.Package.CreateInstallTask(_logger, _wpm, _stringResource));

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<AppManagementViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<AppManagementReviewViewModel>(this);
}
