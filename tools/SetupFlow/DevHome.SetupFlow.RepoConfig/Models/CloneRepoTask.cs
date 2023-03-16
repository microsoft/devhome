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
using DevHome.SetupFlow.Common.Services;
using Microsoft.Extensions.Hosting;
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

    private readonly TaskMessages _taskMessage;

    private readonly ActionCenterErrorMessages _actionCenterMessages;

    private readonly IDeveloperId _developerId;

    public TaskMessages GetLoadingMessages() => _taskMessage;

    public ActionCenterErrorMessages GetActionCenterMessages() => _actionCenterMessages;

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

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

        var stringResource = Application.Current.GetService<StringResource>();
        var executingMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryMainText, repositoryToClone.DisplayName);
        var finishedMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryFinished, repositoryToClone.DisplayName);
        var errorMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryErrorText, repositoryToClone.DisplayName);
        var needsAttention = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningReposityNeedsAttention, repositoryToClone.DisplayName);

        _taskMessage = new TaskMessages(executingMessage, finishedMessage, errorMessage, needsAttention);
        _actionCenterMessages = new ();
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

        var stringResource = Application.Current.GetService<StringResource>();
        var executingMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryMainText, repositoryToClone.DisplayName);
        var finishedMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryFinished, repositoryToClone.DisplayName);
        var errorMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryErrorText, repositoryToClone.DisplayName);
        var needsAttention = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningReposityNeedsAttention, repositoryToClone.DisplayName);

        _taskMessage = new TaskMessages(executingMessage, finishedMessage, errorMessage, needsAttention);
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

            // BUGBUG: Change back to Success when done testing.
            return TaskFinishedState.Failure;
        }).AsAsyncOperation();
    }
}
