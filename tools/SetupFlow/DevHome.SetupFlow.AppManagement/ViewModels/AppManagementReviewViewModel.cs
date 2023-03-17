// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

public partial class AppManagementReviewViewModel : ReviewTabViewModelBase
{
    private readonly ILogger _logger;
    private readonly IStringResource _stringResource;
    private readonly AppManagementTaskGroup _taskGroup;
    private readonly PackageProvider _packageProvider;

    // TODO Use the selected packages for the review list once available.
    public ReadOnlyObservableCollection<PackageViewModel> ReviewPackages => _packageProvider.SelectedPackages;

    public AppManagementReviewViewModel(ILogger logger, IStringResource stringResource, PackageProvider packageProvider, AppManagementTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        _taskGroup = taskGroup;
        _packageProvider = packageProvider;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Applications);
    }
}
