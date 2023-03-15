// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.RepoConfig;

/// <summary>
/// The task group for cloning repositories
/// </summary>
public class RepoConfigTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;

    public RepoConfigTaskGroup(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Gets all the tasks to execute during the loading screen.
    /// </summary>
    public IEnumerable<ISetupTask> SetupTasks => _cloneTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<RepoConfigViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<RepoConfigReviewViewModel>();

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
        _cloneTasks.Clear();
        foreach (var cloningInformation in cloningInformations)
        {
            var fullPath = Path.Combine(cloningInformation.CloningLocation.FullName, cloningInformation.ProviderName, cloningInformation.OwningAccount.LoginId(), cloningInformation.RepositoryToClone.DisplayName());
            _cloneTasks.Add(new CloneRepoTask(new DirectoryInfo(fullPath), cloningInformation.RepositoryToClone, cloningInformation.OwningAccount));
        }
    }
}
