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
    private readonly ISetupFlowStringResource _stringResource;
    private readonly PackageProvider _packageProvider;

    public ReadOnlyObservableCollection<PackageViewModel> ReviewPackages => _packageProvider.SelectedPackages;

    public AppManagementReviewViewModel(
        ILogger logger,
        ISetupFlowStringResource stringResource,
        PackageProvider packageProvider)
    {
        _logger = logger;
        _stringResource = stringResource;
        _packageProvider = packageProvider;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Applications);
    }
}
