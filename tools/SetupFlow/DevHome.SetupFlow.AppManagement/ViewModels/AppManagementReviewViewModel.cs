﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

public partial class AppManagementReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly PackageProvider _packageProvider;

    public ReadOnlyObservableCollection<PackageViewModel> ReviewPackages => _packageProvider.SelectedPackages;

    public AppManagementReviewViewModel(
        ISetupFlowStringResource stringResource,
        PackageProvider packageProvider)
    {
        _stringResource = stringResource;
        _packageProvider = packageProvider;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Applications);
    }
}
