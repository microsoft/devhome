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

    private readonly IDeveloperId _developerId;

    private TaskMessages _taskMessage;

    public TaskMessages GetLoadingMessages() => _taskMessage;

    private ActionCenterMessages _actionCenterMessages;

    public ActionCenterMessages GetErrorMessages() => _actionCenterMessages;

    private ActionCenterMessages _needsAttentionMessages;

    public ActionCenterMessages GetNeedsAttentionMessages() => _needsAttentionMessages;

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
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IDeveloperId developerId, IStringResource stringResource)
    {
        this.cloneLocation = cloneLocation;
        this.repositoryToClone = repositoryToClone;
        _developerId = developerId;
        SetMessages(stringResource);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The reposptyr to clone</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IStringResource stringResource)
    {
        this.cloneLocation = cloneLocation;
        this.repositoryToClone = repositoryToClone;
        SetMessages(stringResource);
    }

    private void SetMessages(IStringResource stringResource)
    {
        var executingMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryMainText, repositoryToClone.DisplayName);
        var finishedMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryFinished, cloneLocation.FullName);
        var errorMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryErrorText, repositoryToClone.DisplayName);
        var needsAttention = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningReposityNeedsAttention, repositoryToClone.DisplayName);
        _taskMessage = new TaskMessages(executingMessage, finishedMessage, errorMessage, needsAttention);

        var errorSubMessage = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryErrorTestSecondary, "Because I force it to fail");
        var errorPrimaryButtonContent = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryErrorMainButtonContent);
        var errorSecondaryButtonContent = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryErrorSecondaryButtonContent);

        var actionCenterErrorMessage = new ActionCenterMessages();
        actionCenterErrorMessage.PrimaryMessage = errorMessage;
        actionCenterErrorMessage.SecondaryMessage = errorSubMessage;
        actionCenterErrorMessage.PrimaryButtonContent = errorPrimaryButtonContent;
        actionCenterErrorMessage.SecondaryButtonContent = errorSecondaryButtonContent;
        _actionCenterMessages = actionCenterErrorMessage;

        var needsAttentionMain = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryNeedsAttentionMainMessage);
        var needsAttentionSub = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryNeedsAttentionSubMessage, repositoryToClone.DisplayName);
        var needsAttentionPrimary = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryNeedsAttentionPrimaryButtonContent);
        var needsAttentionSecondary = stringResource.GetLocalized(StringResourceKey.LoadingScreenCloningRepositoryNeedsAttentionSecondaryButtonContent);

        var needsAttentionMessage = new ActionCenterMessages();
        actionCenterErrorMessage.PrimaryMessage = needsAttentionMain;
        actionCenterErrorMessage.SecondaryMessage = needsAttentionSub;
        actionCenterErrorMessage.PrimaryButtonContent = needsAttentionPrimary;
        actionCenterErrorMessage.SecondaryButtonContent = needsAttentionSecondary;
        _needsAttentionMessages = needsAttentionMessage;
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
}
