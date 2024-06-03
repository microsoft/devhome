// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Linq;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.ViewModels;

public partial class AppManagementReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly PackageProvider _packageProvider;

    public ReadOnlyObservableCollection<PackageViewModel> ReviewPackages => _packageProvider.SelectedPackages;

    public override bool HasItems => _packageProvider.SelectedPackages.Any();

    public AppManagementReviewViewModel(
        ISetupFlowStringResource stringResource,
        PackageProvider packageProvider)
    {
        _stringResource = stringResource;
        _packageProvider = packageProvider;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Applications);
    }
}
