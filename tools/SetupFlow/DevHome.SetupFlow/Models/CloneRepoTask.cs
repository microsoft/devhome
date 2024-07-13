// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Object to hold all information needed to clone a repository.
/// 1:1 CloningInformation to repository.
/// </summary>
public partial class CloneRepoTask : ObservableObject, ISetupTask
{
    private readonly Guid _activityId;

    /// <summary>
    /// Absolute path the user wants to clone their repository to.
    /// </summary>
    private readonly DirectoryInfo _cloneLocation;

    private readonly IRepositoryProvider _repositoryProvider;

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

    [ObservableProperty]
    private bool _isRepoNameTrimmed;

    [RelayCommand]
    public void RepoNameTrimmed()
    {
        IsRepoNameTrimmed = true;
    }

    [ObservableProperty]
    private bool _isClonePathTrimmed;

    [RelayCommand]
    public void ClonePathTrimmed()
    {
        IsClonePathTrimmed = true;
    }

    /// <summary>
    /// Gets the repository in a [organization]\[repo_name] style
    /// </summary>
    public string RepositoryOwnerAndName => Path.Join(RepositoryToClone.OwningAccountName ?? string.Empty, RepositoryToClone.DisplayName);

    private TaskMessages _taskMessage;

    public TaskMessages GetLoadingMessages() => _taskMessage;

    private ActionCenterMessages _actionCenterErrorMessage;

    public ActionCenterMessages GetErrorMessages() => _actionCenterErrorMessage;

    private ActionCenterMessages _needsRebootMessage;

    public ActionCenterMessages GetRebootMessage() => _needsRebootMessage;

    private readonly IStringResource _stringResource;

    // Because AddMessage is defined in ISetupTask every setup task needs to have their own local copy.
    // If a task does not need to add any messages, for example, this class, warning 67 pops up stating that
    // AddMessage event is not used and failing compilation. Adding this pragma suppresses the warning.
    // When this task needs to insert messages into the loading screen this pragma can be removed.
#pragma warning disable 67
    public event ISetupTask.ChangeMessageHandler AddMessage;
#pragma warning restore 67

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
    public CloneRepoTask(IRepositoryProvider repositoryProvider, DirectoryInfo cloneLocation, IRepository repositoryToClone, IDeveloperId developerId, IStringResource stringResource, string providerName, Guid activityId)
    {
        _cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = developerId;
        SetMessages(stringResource);
        ProviderName = providerName;
        _stringResource = stringResource;
        _repositoryProvider = repositoryProvider;
        _activityId = activityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here, at _cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    public CloneRepoTask(IRepositoryProvider repositoryProvider, DirectoryInfo cloneLocation, IRepository repositoryToClone, IStringResource stringResource, string providerName, Guid activityId)
    {
        _cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = null;
        ProviderName = providerName;
        SetMessages(stringResource);
        _stringResource = stringResource;
        _repositoryProvider = repositoryProvider;
        _activityId = activityId;
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

    private void Notify()
    {
        var eventing = Application.Current.GetService<Eventing>();
        var evt = new RepositoryClonedEventArgs
        {
            CloneLocation = CloneLocation.FullName,
            RepositoryName = RepositoryName,
            Repository = RepositoryToClone,
        };
        eventing.OnRepositoryCloned(evt);
    }

    /// <summary>
    /// Clones the repository.
    /// </summary>
    /// <returns>An awaitable operation.</returns>
    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            try
            {
                ProviderOperationResult result;
                Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Cloning repository {RepositoryToClone.DisplayName}");
                TelemetryFactory.Get<ITelemetry>().Log("CloneTask_CloneRepo_Event", LogLevel.Critical, new ReposCloneEvent(ProviderName, _developerId), _activityId);

                if (RepositoryToClone.GetType() == typeof(GenericRepository))
                {
                    await (RepositoryToClone as GenericRepository).CloneRepositoryAsync(_cloneLocation.FullName, null);
                    WasCloningSuccessful = true;
                    Notify();
                    return TaskFinishedState.Success;
                }

                if (_developerId == null)
                {
                    result = await _repositoryProvider.CloneRepositoryAsync(RepositoryToClone, _cloneLocation.FullName);
                }
                else
                {
                    result = await _repositoryProvider.CloneRepositoryAsync(RepositoryToClone, _cloneLocation.FullName, _developerId);
                }

                if (result.Status == ProviderOperationStatus.Failure)
                {
                    _actionCenterErrorMessage.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoErrorForActionCenter, RepositoryToClone.DisplayName, result.DisplayMessage);
                    WasCloningSuccessful = false;
                    return TaskFinishedState.Failure;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.RepoConfig, $"Could not clone {RepositoryToClone.DisplayName}", e);
                _actionCenterErrorMessage.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoErrorForActionCenter, RepositoryToClone.DisplayName, e.HResult.ToString("X", CultureInfo.CurrentCulture));
                TelemetryFactory.Get<ITelemetry>().LogError("CloneTask_CouldNotClone_Event", LogLevel.Critical, new ExceptionEvent(e.HResult));
                return TaskFinishedState.Failure;
            }

            WasCloningSuccessful = true;
            Notify();
            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation) => throw new NotImplementedException();
}
