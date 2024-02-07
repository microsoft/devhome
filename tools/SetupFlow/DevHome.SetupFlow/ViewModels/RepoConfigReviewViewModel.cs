// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Linq;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;

namespace DevHome.SetupFlow.ViewModels;

public partial class RepoConfigReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly RepoConfigTaskGroup _repoTaskGroup;

    public ReadOnlyObservableCollection<CloneRepoTask> RepositoriesToClone =>
        new(new ObservableCollection<CloneRepoTask>(_repoTaskGroup.CloneTasks));

    public override bool HasItems => RepositoriesToClone.Any();

    public RepoConfigReviewViewModel(ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _stringResource = stringResource;
        _repoTaskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
