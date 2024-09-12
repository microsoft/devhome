// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Database.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Object to hold all information needed to clone a repository.
/// 1:1 CloningInformation to repository.
/// </summary>
public partial class CloneRepoTask : ObservableObject, ISetupTask
{
    private readonly IHost _host;

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CloneRepoTask));

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
    /// Gets target device name. Inherited via ISetupTask but unused.
    /// </summary>
    public string TargetName => string.Empty;

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

    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;
#pragma warning restore 67

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

    private readonly CloneRepoSummaryInformationViewModel _summaryScreenInformation;

    public ISummaryInformationViewModel SummaryScreenInformation => _summaryScreenInformation;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here. at _cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    /// <param name="developerId">Credentials needed to clone a private repo</param>
    public CloneRepoTask(
        IRepositoryProvider repositoryProvider,
        DirectoryInfo cloneLocation,
        IRepository repositoryToClone,
        IDeveloperId developerId,
        ISetupFlowStringResource stringResource,
        string providerName,
        Guid activityId,
        IHost host)
    {
        _cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = developerId;
        SetMessages(stringResource);
        ProviderName = providerName;
        _stringResource = stringResource;
        _repositoryProvider = repositoryProvider;
        _activityId = activityId;
        _host = host;
        _summaryScreenInformation = new CloneRepoSummaryInformationViewModel(host.GetService<SetupFlowOrchestrator>(), stringResource);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneRepoTask"/> class.
    /// Task to clone a repository.
    /// </summary>
    /// <param name="cloneLocation">Repository will be placed here, at _cloneLocation.FullName</param>
    /// <param name="repositoryToClone">The repository to clone</param>
    public CloneRepoTask(IRepositoryProvider repositoryProvider, DirectoryInfo cloneLocation, IRepository repositoryToClone, ISetupFlowStringResource stringResource, string providerName, Guid activityId, IHost host)
    {
        _cloneLocation = cloneLocation;
        this.RepositoryToClone = repositoryToClone;
        _developerId = null;
        ProviderName = providerName;
        SetMessages(stringResource);
        _stringResource = stringResource;
        _repositoryProvider = repositoryProvider;
        _activityId = activityId;
        _host = host;
        _summaryScreenInformation = new CloneRepoSummaryInformationViewModel(host.GetService<SetupFlowOrchestrator>(), stringResource);
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
                _log.Information($"Cloning repository {RepositoryToClone.DisplayName}");
                TelemetryFactory.Get<ITelemetry>().Log("CloneTask_CloneRepo_Event", LogLevel.Critical, new RepoCloneEvent(ProviderName, _developerId), _activityId);

                if (RepositoryToClone.GetType() == typeof(GenericRepository))
                {
                    await (RepositoryToClone as GenericRepository).CloneRepositoryAsync(_cloneLocation.FullName, null);

                    WasCloningSuccessful = true;
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
                    _log.Error(result.ExtendedError, $"Could not clone {RepositoryToClone.DisplayName} because {result.DisplayMessage}");
                    TelemetryFactory.Get<ITelemetry>().LogError("CloneTask_CouldNotClone_Event", LogLevel.Critical, new ExceptionEvent(result.ExtendedError.HResult, result.DisplayMessage));

                    _actionCenterErrorMessage.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoErrorForActionCenter, RepositoryToClone.DisplayName, result.DisplayMessage);
                    WasCloningSuccessful = false;
                    return TaskFinishedState.Failure;
                }
            }
            catch (Exception e)
            {
                _log.Error(e, $"Could not clone {RepositoryToClone.DisplayName} because {e.Message}");
                TelemetryFactory.Get<ITelemetry>().LogError("CloneTask_CouldNotClone_Event", LogLevel.Critical, new ExceptionEvent(e.HResult, e.Message));

                _actionCenterErrorMessage.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoErrorForActionCenter, RepositoryToClone.DisplayName, e.Message);
                WasCloningSuccessful = false;
                return TaskFinishedState.Failure;
            }

            // Search for a configuration file.
            var configurationDirectory = Path.Join(_cloneLocation.FullName, DscHelpers.ConfigurationFolderName);
            if (Directory.Exists(configurationDirectory))
            {
                var fileToUse = Directory.EnumerateFiles(configurationDirectory)
                .Where(file => file.EndsWith(DscHelpers.ConfigurationFileYamlExtension, StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(DscHelpers.ConfigurationFileWingetExtension, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(configurationFile => File.GetLastWriteTime(configurationFile))
                .FirstOrDefault();

                if (fileToUse != null)
                {
                    _summaryScreenInformation.FilePathAndName = fileToUse;
                    _summaryScreenInformation.RepoName = RepositoryName;
                    _summaryScreenInformation.OwningAccount = RepositoryToClone.OwningAccountName ?? string.Empty;
                }
            }

            var experimentationService = _host.GetService<IExperimentationService>();
            var canUseTheDatabase = experimentationService.IsFeatureEnabled("RepositoryManagementExperiment");

            if (canUseTheDatabase)
            {
                // TODO: Is this the best place to add the repository to the database?
                // Maybe a "PostExecutionStep" would be nice.
                _host.GetService<RepositoryManagementDataAccessService>()
                .AddRepository(RepositoryName, CloneLocation.FullName);
            }

            WasCloningSuccessful = true;

            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation) => throw new NotImplementedException();
}
