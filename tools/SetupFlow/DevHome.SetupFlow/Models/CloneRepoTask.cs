// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.RepoToolEvents.RepoDialog;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
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
    private readonly DirectoryInfo _cloneLocation;

    public DirectoryInfo CloneLocation => _cloneLocation;

    /// <summary>
    /// Gets the repository the user wants to clone.
    /// </summary>
    public IRepository RepositoryToClone { get; }

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

    private readonly IStringResource _stringResource;

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at _cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    /// <param name="developerId">Credentials needed to clone a private repo</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IDeveloperId developerId, IStringResource stringResource, string providerName)
    {
        _cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = developerId;
        SetMessages(stringResource);
        ProviderName = providerName;
        _stringResource = stringResource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at _cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The reposptyr to clone</param>
    public CloneRepoTask(DirectoryInfo cloneLocation, IRepository repositoryToClone, IStringResource stringResource, string providerName)
    {
        _cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = null;
        ProviderName = providerName;
        SetMessages(stringResource);
        _stringResource = stringResource;
    }

    private void SetMessages(IStringResource stringResource)
    {
        var executingMessage = stringResource.GetLocalized(StringResourceKey.CloneRepoCreating, RepositoryToClone.DisplayName);
        var finishedMessage = stringResource.GetLocalized(StringResourceKey.CloneRepoCreated, _cloneLocation.FullName);
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
            try
            {
                // If the user used the repo tab to add repos then _developerId points to the account used to clone their repo.
                // What if the user used the URL tab?  On a private repo validation is done in the extension to figure out if any
                // logged in account has access to the repo.  However, github plugin does not have a way to tell us what account.
                // _developerId will be null in the case of adding via URL.  _developerId will be null.
                // extension will iterate through all logged in Ids and clone with each one.
                Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Cloning repository {RepositoryToClone.DisplayName}");
                TelemetryFactory.Get<ITelemetry>().Log("CloneTask_CloneRepo_Event", LogLevel.Measure, new ReposCloneEvent(ProviderName, _developerId));
                await RepositoryToClone.CloneRepositoryAsync(_cloneLocation.FullName, _developerId);
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}", e);
                _actionCenterErrorMessage.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoErrorForActionCenter, RepositoryToClone.DisplayName, e.HResult.ToString("X", CultureInfo.CurrentCulture));
                TelemetryFactory.Get<ITelemetry>().LogError("CloneTask_ClouldNotClone_Event", LogLevel.Measure, new ExceptionEvent(e.HResult));
                return TaskFinishedState.Failure;
            }

            WasCloningSuccessful = true;
            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
