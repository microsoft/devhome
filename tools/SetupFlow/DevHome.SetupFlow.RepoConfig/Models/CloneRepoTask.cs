// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Models;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.SetupFlow.RepoConfig.Models;

/// <summary>
/// Object to hold all information needed to clone a repository.
/// 1:1 CloningInformation to repository.
/// </summary>
internal class CloneRepoTask : ISetupTask
{
    /// <summary>
    /// Absolute path the user watns to clone their repository to.
    /// </summary>
    private readonly DirectoryInfo cloneLocation;

    /// <summary>
    /// The repository the user wants to clone.
    /// </summary>
    private readonly IRepository repositoryToClone;

    /// <summary>
    /// Gets a value indicating whether the task requires being admin.
    /// </summary>
    public bool RequiresAdmin => false;

    /// <summary>
    /// Gets a value indicating whether the task requires rebooting their machine.
    /// </summary>
    public bool RequiresReboot => false;

    /// <summary>
    /// THe message to show during the loading screen.
    /// </summary>
    private readonly LoadingMessages _loadingMessage;

    /// <summary>
    /// The developer ID that is used when a repository is being cloned.
    /// </summary>
    private readonly IDeveloperId _developerId;

    /// <summary>
    /// Get all messages to show in the loading screen.
    /// </summary>
    /// <returns>All loading messages for the </returns>
    public LoadingMessages GetLoadingMessages() => _loadingMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository with provided credentials.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    /// <param name="developerId">Credentials needed to clone a private repo</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IDeveloperId developerId)
    {
        this.cloneLocation = cloneLocation;
        this.repositoryToClone = repositoryToClone;
        _developerId = developerId;

        _loadingMessage = new ("Cloning Repository " + repositoryToClone.DisplayName(),
            "Done cloning repository " + repositoryToClone.DisplayName(),
            "Something happened to repository " + repositoryToClone.DisplayName() + ", oh no!",
            "Repository " + repositoryToClone.DisplayName() + " needs your attention.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The reposptyr to clone</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone)
    {
        this.cloneLocation = cloneLocation;
        this.repositoryToClone = repositoryToClone;

        _loadingMessage = new ("Cloning Repository " + repositoryToClone.DisplayName(),
            "Done cloning repository " + repositoryToClone.DisplayName(),
            "Something happened to repository " + repositoryToClone.DisplayName() + ", oh no!",
            "Repository " + repositoryToClone.DisplayName() + " needs your attention.");
    }

    /// <summary>
    /// Clones the repository.  Mkes the directory if it does not exist.
    /// </summary>
    /// <returns>An awaitable operation.</returns>
    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            if (!cloneLocation.Exists)
            {
                try
                {
                    Directory.CreateDirectory(cloneLocation.FullName);
                }
                catch (Exception)
                {
                    return TaskFinishedState.Failure;
                }
            }

            await repositoryToClone.CloneRepositoryAsync(cloneLocation.FullName, _developerId);
            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }
}
