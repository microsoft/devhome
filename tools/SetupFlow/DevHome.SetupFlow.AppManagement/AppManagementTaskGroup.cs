// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
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
    private readonly WindowsPackageManagerFactory _wingetFactory;

    public AppManagementTaskGroup(
        IHost host,
        IWindowsPackageManager wpm,
        PackageProvider packageProvider,
        ISetupFlowStringResource stringResource,
        ILogger logger,
        WindowsPackageManagerFactory wingetFactory)
    {
        _host = host;
        _packageProvider = packageProvider;
        _wpm = wpm;
        _stringResource = stringResource;
        _logger = logger;
        _wingetFactory = wingetFactory;

        // TODO Convert the package provider to a scoped instance, to avoid
        // clearing it here. This requires refactoring and adding scopes when
        // creating task groups in the main page.
        _packageProvider.Clear();
    }

    public IEnumerable<ISetupTask> SetupTasks => _packageProvider.SelectedPackages
        .Select(sp => sp.Package.CreateInstallTask(_logger, _wpm, _stringResource, _wingetFactory));

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<AppManagementViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<AppManagementReviewViewModel>(this);
}
