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

/// <summary>
/// Task that creates an environment using the user input from an adaptive card session.
/// </summary>
public sealed class CreateEnvironmentTask : ISetupTask, IDisposable, IRecipient<CreationAdaptiveCardSessionEndedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CreateEnvironmentTask));

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly TaskMessages _taskMessages;

    private readonly ActionCenterMessages _actionCenterMessages = new();

    private readonly ISetupFlowStringResource _stringResource;

    // Used to signal the task to start the creation operation. This is used when the adaptive card session ends.
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

    public ISummaryInformationViewModel SummaryScreenInformation { get; }

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

        // Json input that the user entered in the adaptive card session
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
                // Either wait until we're signalled to continue execution or we times out after 2 minutes. If this task is initiated
                // then that means the user went past the review page. At this point the extension should be firing a session ended
                // event. Since the call flow is disjointed an extension may not have sent the session ended event when this method is called.
                _autoResetEventToStartCreationOperation.WaitOne(TimeSpan.FromMinutes(1));

                // if we time out then that means the extension didn't send the session ended event. So there is no point in trying again
                // as the error will happen again.
                _isFirstAttempt = false;
            }

            if (string.IsNullOrWhiteSpace(UserJsonInput))
            {
                _log.Information("UserJsonInput is null or empty");
            }

            // If the provider details are null, then we can't proceed with the operation. This happens if the auto event times out.
            if (ProviderDetails == null)
            {
                _log.Error("ProviderDetails is null");
                AddMessage(_stringResource.GetLocalized(StringResourceKey.EnvironmentCreationFailedToGetProviderInformation), MessageSeverityKind.Error);
                return TaskFinishedState.Failure;
            }

            var sdkCreateEnvironmentOperation = ProviderDetails.ComputeSystemProvider.CreateCreateComputeSystemOperation(DeveloperIdWrapper.DeveloperId, UserJsonInput);
            var createComputeSystemOperationWrapper = new CreateComputeSystemOperation(sdkCreateEnvironmentOperation, ProviderDetails, UserJsonInput);

            // Start the operation, which returns immediately and runs in the background.
            createComputeSystemOperationWrapper.StartOperation();
            AddMessage(_stringResource.GetLocalized(StringResourceKey.EnvironmentCreationForProviderStarted), MessageSeverityKind.Info);

            _computeSystemManager.AddRunningOperationForCreation(createComputeSystemOperationWrapper);
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
