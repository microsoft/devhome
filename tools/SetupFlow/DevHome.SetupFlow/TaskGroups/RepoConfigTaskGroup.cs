// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.SetupFlow.TaskGroups;

/// <summary>
/// The task group for cloning repositories
/// </summary>
public class RepoConfigTaskGroup : ISetupTaskGroup
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepoConfigTaskGroup));
    private readonly IHost _host;
    private readonly Lazy<RepoConfigReviewViewModel> _repoConfigReviewViewModel;
    private readonly Lazy<RepoConfigViewModel> _repoConfigViewModel;
    private readonly Guid _activityId;

    private readonly ISetupFlowStringResource _stringResource;

    public RepoConfigTaskGroup(IHost host, ISetupFlowStringResource stringResource, SetupFlowOrchestrator setupFlowOrchestrator, IDevDriveManager devDriveManager)
    {
        _host = host;
        _stringResource = stringResource;

        // TODO https://github.com/microsoft/devhome/issues/631
        _repoConfigViewModel = new(() => new RepoConfigViewModel(stringResource, setupFlowOrchestrator, devDriveManager, this, host));
        _repoConfigReviewViewModel = new(() => new RepoConfigReviewViewModel(stringResource, this));
        _activityId = setupFlowOrchestrator.ActivityId;
    }

    /// <summary>
    /// Gets all the tasks to execute during the loading screen.
    /// </summary>
    public IEnumerable<ISetupTask> SetupTasks => CloneTasks;

    public IEnumerable<ISetupTask> DSCTasks => SetupTasks;

    /// <summary>
    /// Gets all tasks that need to be ran.
    /// </summary>
    public IList<CloneRepoTask> CloneTasks { get; } = new List<CloneRepoTask>();

    public SetupPageViewModelBase GetSetupPageViewModel() => _repoConfigViewModel.Value;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _repoConfigReviewViewModel.Value;

    /// <summary>
    /// Converts CloningInformation to a CloneRepoTask.
    /// </summary>
    /// <param name="cloningInformations">all repositories the user wants to clone.</param>
    public void SaveSetupTaskInformation(List<CloningInformation> cloningInformations)
    {
        _log.Information("Saving cloning information to task group");
        CloneTasks.Clear();

        List<FinalRepoResult> allAddedRepos = new();

        foreach (var cloningInformation in cloningInformations)
        {
            // if the repo was added via URL.
            CloneRepoTask task;
            if (cloningInformation.OwningAccount == null)
            {
                task = new CloneRepoTask(cloningInformation.RepositoryProvider, new DirectoryInfo(cloningInformation.ClonePath), cloningInformation.RepositoryToClone, _stringResource, cloningInformation.RepositoryProviderDisplayName, _activityId, _host);
            }
            else
            {
                task = new CloneRepoTask(cloningInformation.RepositoryProvider, new DirectoryInfo(cloningInformation.ClonePath), cloningInformation.RepositoryToClone, cloningInformation.OwningAccount, _stringResource, cloningInformation.RepositoryProviderDisplayName, _activityId, _host);
            }

            if (cloningInformation.CloneToDevDrive)
            {
                task.DependsOnDevDriveToBeInstalled = true;
            }

            CloneTasks.Add(task);

            // Perform telemetry work.
            var providerName = cloningInformation.ProviderName;
            var addKind = cloningInformation.OwningAccount == null ? AddKind.URL : AddKind.Account;
            var cloneLocationKind = CloneLocationKind.LocalPath;
            if (cloningInformation.CloneToExistingDevDrive || cloningInformation.CloneToDevDrive)
            {
                cloneLocationKind = CloneLocationKind.DevDrive;
            }

            allAddedRepos.Add(new FinalRepoResult(providerName, addKind, cloneLocationKind));
        }
    }
}
