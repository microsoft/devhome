// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

public partial class RepoConfigReviewViewModel : ReviewTabViewModelBase
{
    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly RepoConfigTaskGroup _taskGroup;
    private readonly ReadOnlyObservableCollection<string> _repositoriesToClone;

    public ReadOnlyObservableCollection<string> RepositoriesToClone => _repositoriesToClone;

    public RepoConfigReviewViewModel(ILogger logger, ISetupFlowStringResource stringResource, RepoConfigTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        _taskGroup = taskGroup;

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }

    public RepoConfigReviewViewModel(ILogger logger, ISetupFlowStringResource stringResource, List<CloneRepoTask> cloningTasks)
    {
        _logger = logger;
        _stringResource = stringResource;
        _repositoriesToClone = new ReadOnlyObservableCollection<string>(
            new ObservableCollection<string>(cloningTasks.Select(x => x.RepositoryToClone.DisplayName)));

        TabTitle = stringResource.GetLocalized(StringResourceKey.Repository);
    }
}
