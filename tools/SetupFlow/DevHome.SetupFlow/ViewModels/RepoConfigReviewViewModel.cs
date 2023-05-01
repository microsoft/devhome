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
    private readonly RepoConfigTaskGroup _repoTaskGroup;

    public ReadOnlyObservableCollection<string> RepositoriesToClone =>
        new (new ObservableCollection<string>(_repoTaskGroup.CloneTasks.Select(x => x.CloneLocation.FullName)));

    public override bool HasItems => RepositoriesToClone.Any();

    public RepoConfigReviewViewModel(ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _stringResource = stringResource;
        _repoTaskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
