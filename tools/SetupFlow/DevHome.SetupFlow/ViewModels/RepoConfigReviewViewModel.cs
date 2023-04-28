// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;

namespace DevHome.SetupFlow.ViewModels;

public partial class RepoConfigReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly ReadOnlyObservableCollection<string> _repositoriesToClone;

    public ReadOnlyObservableCollection<string> RepositoriesToClone => _repositoriesToClone;

    public override bool HasItems => _repositoriesToClone.Any();

    public RepoConfigReviewViewModel(ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _stringResource = stringResource;
        _repositoriesToClone = new ReadOnlyObservableCollection<string>(
            new ObservableCollection<string>(taskGroup.CloneTasks.Select(x => x.CloneLocation.FullName)));

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
