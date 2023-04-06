// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;

namespace DevHome.SetupFlow.ViewModels;

public partial class RepoConfigReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly RepoConfigTaskGroup _taskGroup;
    private readonly ReadOnlyObservableCollection<string> _repositoriesToClone;

    public ReadOnlyObservableCollection<string> RepositoriesToClone => _repositoriesToClone;

    public RepoConfigReviewViewModel(ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _stringResource = stringResource;
        _taskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }

    public RepoConfigReviewViewModel(ISetupFlowStringResource stringResource, List<CloneRepoTask> cloningTasks)
    {
        _stringResource = stringResource;
        _repositoriesToClone = new ReadOnlyObservableCollection<string>(
            new ObservableCollection<string>(cloningTasks.Select(x => x.RepositoryToClone.DisplayName)));

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
