// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;
using Windows.Foundation;
using DevHomeSDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Task that creates an environment using the user input from an adaptive card session.
/// </summary>
public sealed class CreateEnvironmentTask : ISetupTask, IDisposable, IRecipient<CreationAdaptiveCardSessionEndedMessage>, IRecipient<CreationProviderChangedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CreateEnvironmentTask));

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly TaskMessages _taskMessages;

    private readonly ActionCenterMessages _actionCenterMessages = new();

    private readonly ISetupFlowStringResource _stringResource;

    // Used to signal the task to start the creation operation. This is used when the adaptive card session ends.
    private readonly AutoResetEvent _autoResetEventToStartCreationOperation = new(false);

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly SetupFlowOrchestrator _orchestrator;

    private bool _hasAdaptiveCardSessionFinished;

    private bool _disposedValue;

    public event ISetupTask.ChangeMessageHandler AddMessage;

    public string UserJsonInput { get; set; }

    public ComputeSystemProviderDetails ProviderDetails { get; set; }

    public DeveloperIdWrapper DeveloperIdWrapper { get; set; }

    // The "#pragma warning disable 67" directive suppresses the CS0067 warning.
    // CS0067 is a warning that occurs when a public event is declared but never used.
#pragma warning disable 67
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;
#pragma warning restore 67

    public bool RequiresAdmin => false;

    public bool RequiresReboot => false;

    /// <summary>
    /// Gets target device name. Inherited via ISetupTask but unused.
    /// </summary>
    public string TargetName => string.Empty;

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
        _orchestrator = setupFlowViewModel.Orchestrator;

        // Register for the adaptive card session ended message so we can use the session data to create the environment
        WeakReferenceMessenger.Default.Register<CreationAdaptiveCardSessionEndedMessage>(this);
        WeakReferenceMessenger.Default.Register<CreationProviderChangedMessage>(this);
    }

    public ActionCenterMessages GetErrorMessages() => _actionCenterMessages;

    public TaskMessages GetLoadingMessages() => _taskMessages;

    public ActionCenterMessages GetRebootMessage() => new();

    /// <summary>
    /// Receives the adaptive card session ended message from the he <see cref="ViewModels.Environments.EnvironmentCreationOptionsViewModel"/>
    /// once the extension sends the session ended event.
    /// </summary>
    /// <param name="message">
    /// The message payload that contains the provider and the user input json that will be used to invoke the
    /// <see cref="DevHomeSDK.IComputeSystemProvider.CreateCreateComputeSystemOperation(DevHomeSDK.IDeveloperId, string)"/>
    /// </param>
    public void Receive(CreationAdaptiveCardSessionEndedMessage message)
    {
        _log.Information("The extension sent the session ended event");
        _hasAdaptiveCardSessionFinished = true;

        // Json input that the user entered in the adaptive card session
        UserJsonInput = message.Value.UserInputResultJson;

        // In the future we'll add the specific developer ID to the task, but for now since we haven't
        // add support for switching between developer Id's in the environments pages, we'll use the first one
        // in the provider details list of developer IDs. If we get here, then there should be at least one.
        DeveloperIdWrapper = message.Value.ProviderDetails.DeveloperIds.First();

        _log.Information("Signaling to the waiting event handle to Continue the 'Execute' operation");
        _autoResetEventToStartCreationOperation.Set();
    }

    private void OnEndSetupFlow(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CreationAdaptiveCardSessionEndedMessage>(this);
        _setupFlowViewModel.EndSetupFlow -= OnEndSetupFlow;
    }

    IAsyncOperationWithProgress<TaskFinishedState, TaskProgress> ISetupTask.Execute()
    {
        return AsyncInfo.Run<TaskFinishedState, TaskProgress>(async (_, _) =>
        {
            _log.Information("Executing the operation. Waiting to be signalled that the adaptive card session has ended");

            // Either wait until we're signaled to continue execution or we times out after 1 minute. If this task is initiated
            // then that means the user went past the review page. At this point the extension should be firing a session ended
            // event. Since the call flow is disjointed an extension may not have sent the session ended event when this method is called.
            _autoResetEventToStartCreationOperation.WaitOne(TimeSpan.FromMinutes(1));

            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_Creation_Event",
                LogLevel.Critical,
                new EnvironmentCreationEvent(ProviderDetails.ComputeSystemProvider.Id, EnvironmentsTelemetryStatus.Started, new TelemetryResult()),
                _orchestrator.ActivityId);

            if (string.IsNullOrWhiteSpace(UserJsonInput))
            {
                // The extension's creation adaptive card may not need user input. In that case, the user input will be null or empty.
                _log.Information("UserJsonInput is null or empty.");
            }

            // The extension did not send us back a response that the session has finished. This happens if the auto event times out
            // while we're still waiting for the extension to send the stopped event.
            if (!_hasAdaptiveCardSessionFinished)
            {
                var logErrorMsg = $"Timed out waiting for the {ProviderDetails.ComputeSystemProvider.Id} provider to stop the adaptive card session";
                _log.Error(logErrorMsg);

                var failureResult = new ProviderOperationResult(ProviderOperationStatus.Failure, new TimeoutException(), logErrorMsg, logErrorMsg);
                var providerId = ProviderDetails.ComputeSystemProvider.Id;
                TelemetryFactory.Get<ITelemetry>().Log(
                    "Environment_Creation_Event",
                    LogLevel.Critical,
                    new EnvironmentCreationEvent(providerId, EnvironmentsTelemetryStatus.Failed, new TelemetryResult(failureResult)),
                    _orchestrator.ActivityId);

                AddMessage(_stringResource.GetLocalized(StringResourceKey.EnvironmentCreationFailedToGetProviderInformation), MessageSeverityKind.Error);
                return TaskFinishedState.Failure;
            }

            var sdkCreateEnvironmentOperation = ProviderDetails.ComputeSystemProvider.CreateCreateComputeSystemOperation(DeveloperIdWrapper.DeveloperId, UserJsonInput);
            var createComputeSystemOperationWrapper = new CreateComputeSystemOperation(
                sdkCreateEnvironmentOperation,
                ProviderDetails,
                UserJsonInput,
                _orchestrator.ActivityId);

            // Start the operation, which returns immediately and runs in the background.
            createComputeSystemOperationWrapper.StartOperation();
            AddMessage(_stringResource.GetLocalized(StringResourceKey.EnvironmentCreationForProviderStarted), MessageSeverityKind.Info);

            _computeSystemManager.AddRunningOperationForCreation(createComputeSystemOperationWrapper);
            CreationOperationStarted = true;

            _log.Information("Successfully started the creation operation");
            await Task.CompletedTask;
            return TaskFinishedState.Success;
        });
    }

    IAsyncOperationWithProgress<TaskFinishedState, TaskProgress> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation)
    {
        return AsyncInfo.Run<TaskFinishedState, TaskProgress>(async (_, progress) =>
        {
            // No admin rights required for this task. This shouldn't ever be invoked since the RequiresAdmin property is always false.
            _log.Error("Admin execution is not required for the create environment task");
            await Task.CompletedTask;
            return TaskFinishedState.Failure;
        });
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

    /// <summary>
    /// Weak reference message handler for when the selected provider changes in the select environment provider page. This will be triggered when the user
    /// selects an item in the Select Environment Provider page.
    /// </summary>
    /// <param name="message">Message data that contains the new provider details.</param>
    public void Receive(CreationProviderChangedMessage message)
    {
        ProviderDetails = message.Value;
    }
}
