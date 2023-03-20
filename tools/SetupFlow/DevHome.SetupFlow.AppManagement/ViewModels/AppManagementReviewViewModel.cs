// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

public partial class AppManagementReviewViewModel : ReviewTabViewModelBase
{
    private readonly ILogger _logger;
    private readonly IStringResource _stringResource;
    private readonly AppManagementTaskGroup _taskGroup;

    // TODO Use the selected packages for the review list once available.
    public ObservableCollection<PackageViewModel> ReviewPackages { get; } = new ();

    public AppManagementReviewViewModel(ILogger logger, SetupFlowStringResource stringResource, AppManagementTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        _taskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Applications);
    }
}
