// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

public sealed class CreateEnvironmentTask : ISetupTask, IDisposable, IRecipient<CreationAdaptiveCardSessionEndedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CreateEnvironmentTask));

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly TaskMessages _taskMessages;

    private readonly ActionCenterMessages _actionCenterMessages = new();

    private readonly ISetupFlowStringResource _stringResource;

    private readonly AutoResetEvent _autoResetEventToStartCreationOperation = new(false);

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private bool _disposedValue;

    private bool _isFirstAttempt;

    public event ISetupTask.ChangeMessageHandler AddMessage;

    public string UserJsonInput { get; set; }

    public ComputeSystemProviderDetails ProviderDetails { get; set; }

    public DeveloperIdWrapper DeveloperIdWrapper { get; set; }

#pragma warning disable 67
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;
#pragma warning restore 67

    public bool RequiresAdmin => false;

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled => false;

    public bool CreationOperationStarted { get; private set; }

    public CreateEnvironmentTask(IComputeSystemManager computeSystemManager, ISetupFlowStringResource stringResource, SetupFlowViewModel setupFlowViewModel)
    {
        _computeSystemManager = computeSystemManager;
        _stringResource = stringResource;
        _taskMessages = new TaskMessages
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.StartingEnvironmentCreation),
            Finished = _stringResource.GetLocalized(StringResourceKey.EnvironmentCreationOperationInitializationFinished),
            Error = _stringResource.GetLocalized(StringResourceKey.EnvironmentCreationError),
        };
        _setupFlowViewModel = setupFlowViewModel;
        _setupFlowViewModel.EndSetupFlow += OnEndSetupFlow;

        // Register for the adaptive card session ended message so we can use the session data to create the environment
        WeakReferenceMessenger.Default.Register<CreationAdaptiveCardSessionEndedMessage>(this);
        _isFirstAttempt = true;
    }

    public ActionCenterMessages GetErrorMessages() => _actionCenterMessages;

    public TaskMessages GetLoadingMessages() => _taskMessages;

    public ActionCenterMessages GetRebootMessage() => new();

    public void Receive(CreationAdaptiveCardSessionEndedMessage message)
    {
        ProviderDetails = message.Value.ProviderDetails;
        UserJsonInput = message.Value.UserInputResultJson;

        // In the future we'll add the specific developer ID to the task, but for now since we haven't
        // add support for switching between developer Id's in the environments pages, we'll use the first one
        // in the provider details list of developer IDs. If we get here, then there should be at least one.
        DeveloperIdWrapper = message.Value.ProviderDetails.DeveloperIds.First();

        _autoResetEventToStartCreationOperation.Set();
    }

    private void OnEndSetupFlow(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CreationAdaptiveCardSessionEndedMessage>(this);
        _setupFlowViewModel.EndSetupFlow -= OnEndSetupFlow;
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(() =>
        {
            if (_isFirstAttempt)
            {
                // Either wait until either we're signalled to continue execution or time out after a minute. This gives enough time for the
                // extension to send the stopped event for the adaptive card session.
                _autoResetEventToStartCreationOperation.WaitOne(TimeSpan.FromMinutes(1));
                _isFirstAttempt = false;
            }

            if (string.IsNullOrWhiteSpace(UserJsonInput))
            {
                _log.Information("UserJsonInput is null or empty");
            }

            if (ProviderDetails == null)
            {
                _log.Error("ProviderDetails is null");
                AddMessage(_stringResource.GetLocalized(StringResourceKey.EnvironmentCreationFailedToGetProviderInformation, ProviderDetails.ComputeSystemProvider.DisplayName), MessageSeverityKind.Error);
                return TaskFinishedState.Failure;
            }

            // var sdkCreateEnvironmentOperation = ProviderDetails.ComputeSystemProvider.CreateCreateComputeSystemOperation(DeveloperIdWrapper.DeveloperId, UserJsonInput);
            // var createComputeSystemOperationWrapper = new CreateComputeSystemOperation(sdkCreateEnvironmentOperation, ProviderDetails, UserJsonInput);

            // Create a cancellation token that will cancel the task after 2 hours. Dev Box creation depends on the organization azure subscription. So depending on the
            // subscription and pool for example it could take over 65 minutes. Setting the timeout to 2 hours, should cover most cases. This can be adjusted in the future.
            // var cancellationTokenSource = new CancellationTokenSource();
            // cancellationTokenSource.CancelAfter(TimeSpan.FromHours(2));

            // Start the operation, which returns immediately and runs in the background.
            // createComputeSystemOperationWrapper.StartOperation(cancellationTokenSource.Token);
            AddMessage(_stringResource.GetLocalized(StringResourceKey.EnvironmentCreationForProviderStarted, ProviderDetails.ComputeSystemProvider.DisplayName), MessageSeverityKind.Info);

            // _computeSystemManager.AddRunningOperationForCreation(createComputeSystemOperationWrapper);
            CreationOperationStarted = true;
            return TaskFinishedState.Success;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation)
    {
        return Task.Run(() =>
        {
            // No admin rights required for this task
            _log.Error("Admin execution is not required for the create environment task");
            return TaskFinishedState.Failure;
        }).AsAsyncOperation();
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _autoResetEventToStartCreationOperation.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
