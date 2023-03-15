// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.ElevatedComponent;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;

namespace DevHome.SetupFlow.RepoConfig.Models;

internal class CloneRepoTask : ISetupTask
{
    private readonly DirectoryInfo cloneLocation;

    private readonly IRepository repositoryToClone;

    public bool RequiresAdmin => false;

    public bool RequiresReboot => false;

    private readonly LoadingMessages _loadingMessage;

    private readonly IDeveloperId _developerId;

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

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
