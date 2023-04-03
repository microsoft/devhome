// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.ElevatedComponent;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.SetupFlow.RepoConfig.Models;

/// <summary>
/// Object to hold all information needed to clone a repository.
/// 1:1 CloningInformation to repository.
/// </summary>
public class CloneRepoTask : ISetupTask
{
    /// <summary>
    /// Absolute path the user wants to clone their repository to.
    /// </summary>
    private readonly DirectoryInfo cloneLocation;

    /// <summary>
    /// Gets the repository the user wants to clone.
    /// </summary>
    public IRepository RepositoryToClone { get; }

    /// <summary>
    /// Gets a value indicating whether the task requires being admin.
    /// </summary>
    public bool RequiresAdmin => false;

    /// <summary>
    /// Gets a value indicating whether the task requires rebooting their machine.
    /// </summary>
    public bool RequiresReboot => false;

    /// <summary>
    /// The developer ID that is used when a repository is being cloned.
    /// </summary>
    private readonly IDeveloperId _developerId;

    private TaskMessages _taskMessage;

    public TaskMessages GetLoadingMessages() => _taskMessage;

    private ActionCenterMessages _actionCenterErrorMessage;

    public ActionCenterMessages GetErrorMessages() => _actionCenterErrorMessage;

    private ActionCenterMessages _needsRebootMessage;

    public ActionCenterMessages GetRebootMessage() => _needsRebootMessage;

    public bool DependsOnDevDriveToBeInstalled
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    /// <param name="developerId">Credentials needed to clone a private repo</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IDeveloperId developerId, IStringResource stringResource)
    {
        this.cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
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
        this.RepositoryToClone = repositoryToClone;
        SetMessages(stringResource);
    }

    private void SetMessages(IStringResource stringResource)
    {
        var executingMessage = stringResource.GetLocalized(StringResourceKey.CloneRepoCreating, RepositoryToClone.DisplayName);
        var finishedMessage = stringResource.GetLocalized(StringResourceKey.CloneRepoCreated, cloneLocation.FullName);
        var errorMessage = stringResource.GetLocalized(StringResourceKey.CloneRepoError, RepositoryToClone.DisplayName);
        var needsRebootMessage = stringResource.GetLocalized(StringResourceKey.CloneRepoRestart, RepositoryToClone.DisplayName);
        _taskMessage = new TaskMessages(executingMessage, finishedMessage, errorMessage, needsRebootMessage);

        var actionCenterErrorMessage = new ActionCenterMessages();
        actionCenterErrorMessage.PrimaryMessage = errorMessage;
        _actionCenterErrorMessage = actionCenterErrorMessage;

        _needsRebootMessage = new ActionCenterMessages();
        _needsRebootMessage.PrimaryMessage = needsRebootMessage;
    }

    /// <summary>
    /// Clones the repository.  Makes the directory if it does not exist.
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

            try
            {
                await RepositoryToClone.CloneRepositoryAsync(cloneLocation.FullName, _developerId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something happened while trying to clone {cloneLocation.FullName}");
                Console.WriteLine(e.ToString());
                return TaskFinishedState.Failure;
            }

            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
