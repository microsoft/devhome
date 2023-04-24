// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

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
    public IRepository RepositoryToClone
    {
        get;
    }

    /// <summary>
    /// Gets the display name of the repository.
    /// </summary>
    public string RepositoryName => RepositoryToClone.DisplayName;

    /// <summary>
    /// Gets the provider name the repository is cloning from.
    /// </summary>
    public string ProviderName
    {
        get; private set;
    }

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

    // May potentially be moved to a central list in the future.
    public bool WasCloningSuccessful
    {
        get; private set;
    }

    private TaskMessages _taskMessage;

    public TaskMessages GetLoadingMessages() => _taskMessage;

    private ActionCenterMessages _actionCenterErrorMessage;

    public ActionCenterMessages GetErrorMessages() => _actionCenterErrorMessage;

    private ActionCenterMessages _needsRebootMessage;

    public ActionCenterMessages GetRebootMessage() => _needsRebootMessage;

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    /// <param name="developerId">Credentials needed to clone a private repo</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IDeveloperId developerId, IStringResource stringResource, string providerName)
    {
        this.cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = developerId;
        SetMessages(stringResource);
        ProviderName = providerName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The reposptyr to clone</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IStringResource stringResource, string providerName)
    {
        this.cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = null;
        ProviderName = providerName;
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
    /// Clones the repository.
    /// </summary>
    /// <returns>An awaitable operation.</returns>
    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            // If the user used the repo tab to add repos then _developerId points to the account used to clone their repo.
            // What if the user used the URL tab?  On a private repo validation is done in the extension to figure out if any
            // logged in account has access to the repo.  However, github plugin does not have a way to tell us what account.
            // _developerId will be null in the case of adding via URL.  _developerId will be null.
            // extension will iterate through all logged in Ids and clone with each one.
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Cloning repository {RepositoryToClone.DisplayName}");

            try
            {
                await RepositoryToClone.CloneRepositoryAsync(cloneLocation.FullName, _developerId);
            }
            catch (DirectoryNotFoundException)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}");
                _actionCenterErrorMessage.PrimaryMessage = "Path is not empty.";
                return TaskFinishedState.Failure;
            }
            catch (UnauthorizedAccessException)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}");
                _actionCenterErrorMessage.PrimaryMessage = "Repo private or not found.";
                return TaskFinishedState.Failure;
            }
            catch (FileNotFoundException)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}");
                _actionCenterErrorMessage.PrimaryMessage = "Can't clone all submodules.";
                return TaskFinishedState.Failure;
            }
            catch (OperationCanceledException)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}");
                _actionCenterErrorMessage.PrimaryMessage = "User cancelled.";
                return TaskFinishedState.Failure;
            }
            catch (InvalidOperationException)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}");
                _actionCenterErrorMessage.PrimaryMessage = "Cloning failed.";
                return TaskFinishedState.Failure;
            }

            WasCloningSuccessful = true;
            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
