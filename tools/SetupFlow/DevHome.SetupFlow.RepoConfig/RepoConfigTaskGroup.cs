// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.RepoConfig;

public class RepoConfigTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;

    public RepoConfigTaskGroup(IHost host)
    {
        _host = host;
    }

    private readonly IList<CloneRepoTask> _cloneTasks = new List<CloneRepoTask>();

    public IEnumerable<ISetupTask> SetupTasks => _cloneTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _host.CreateInstance<RepoConfigViewModel>(this);

    public ReviewTabViewModelBase GetReviewTabViewModel() => _host.CreateInstance<RepoConfigReviewViewModel>();

    public void SaveSetupTaskInformation(CloningInformation cloningInformation)
    {
        foreach (var developerId in cloningInformation.RepositoriesToClone.Keys)
        {
            foreach (var repositoryToClone in cloningInformation.RepositoriesToClone[developerId])
            {
                // Possible that two accounts have the same repo name from forking.
                var fullPath = Path.Combine(cloningInformation.CloneLocation.FullName, developerId.LoginId(), repositoryToClone.DisplayName());
                _cloneTasks.Add(new CloneRepoTask(new DirectoryInfo(fullPath), repositoryToClone, developerId));
            }
        }
    }

    public void SaveSetupTaskInformation(DirectoryInfo path, IRepository repoToClone)
    {
        var fullPath = Path.Combine(path.FullName, repoToClone.DisplayName());
        _cloneTasks.Add(new CloneRepoTask(new DirectoryInfo(fullPath), repoToClone));
    }
}
