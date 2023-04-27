// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.TaskGroups;

/// <summary>
/// The task group for cloning repositories
/// </summary>
public class RepoConfigTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;
    private readonly Lazy<RepoConfigReviewViewModel> _repoConfigReviewViewModel;
    private readonly Lazy<RepoConfigViewModel> _repoConfigViewModel;

    private readonly ISetupFlowStringResource _stringResource;

    public RepoConfigTaskGroup(IHost host, ISetupFlowStringResource stringResource)
    {
        _host = host;
        _stringResource = stringResource;

        // TODO Remove `this` argument from CreateInstance since this task
        // group is a registered type. This requires updating dependent classes
        // correspondingly.
        _repoConfigViewModel = new (() => _host.CreateInstance<RepoConfigViewModel>(this));
        _repoConfigReviewViewModel = new (() => _host.CreateInstance<RepoConfigReviewViewModel>(this));
    }

    /// <summary>
    /// Gets all the tasks to execute during the loading screen.
    /// </summary>
    public IEnumerable<ISetupTask> SetupTasks => _cloneTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _repoConfigViewModel.Value;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<RepoConfigReviewViewModel>(_cloneTasks);

    /// <summary>
    /// All tasks that need to be ran.
    /// </summary>
    private readonly IList<CloneRepoTask> _cloneTasks = new List<CloneRepoTask>();

    /// <summary>
    /// Converts CloningInformation to a CloneRepoTask.
    /// </summary>
    /// <param name="cloningInformations">all repositories the user wants to clone.</param>
    public void SaveSetupTaskInformation(List<CloningInformation> cloningInformations)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Saving cloning information to task group");
        _cloneTasks.Clear();
        foreach (var cloningInformation in cloningInformations)
        {
            // if the repo was added via URL.
            CloneRepoTask task;
            if (cloningInformation.OwningAccount == null)
            {
                task = new CloneRepoTask(new DirectoryInfo(cloningInformation.ClonePath), cloningInformation.RepositoryToClone, _stringResource, cloningInformation.ProviderName);
            }
            else
            {
                task = new CloneRepoTask(new DirectoryInfo(cloningInformation.ClonePath), cloningInformation.RepositoryToClone, cloningInformation.OwningAccount, _stringResource, cloningInformation.ProviderName);
            }

            if (cloningInformation.CloneToDevDrive)
            {
                task.DependsOnDevDriveToBeInstalled = true;
            }

            _cloneTasks.Add(task);
        }
    }
}
