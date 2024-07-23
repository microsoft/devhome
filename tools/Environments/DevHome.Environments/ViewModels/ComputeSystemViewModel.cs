// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.Environments;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Environments.Helpers;
using DevHome.Environments.Models;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinUIEx;
using WinUIEx.Messaging;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// View model for a compute system. Each 'card' in the UI represents a compute system.
/// Contains an instance of the compute system object as well.
/// </summary>
public partial class ComputeSystemViewModel : ComputeSystemCardBase, IRecipient<ComputeSystemOperationStartedMessage>, IRecipient<ComputeSystemOperationCompletedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemViewModel));

    private readonly StringResource _stringResource;

    private readonly WindowEx _windowEx;

    private readonly IComputeSystemManager _computeSystemManager;

    // Launch button operations
    public ObservableCollection<OperationsViewModel> LaunchOperations { get; set; } = new();

    public ObservableCollection<CardProperty> Properties { get; set; } = new();

    public string PackageFullName { get; set; }

    private readonly Func<ComputeSystemCardBase, bool> _removalAction;

    public ComputeSystemViewModel(
        IComputeSystemManager manager,
        IComputeSystem system,
        ComputeSystemProvider provider,
        Func<ComputeSystemCardBase, bool> removalAction,
        string packageFullName,
        WindowEx windowEx)
    {
        _windowEx = windowEx;
        _computeSystemManager = manager;

        ComputeSystem = new(system);
        ProviderDisplayName = provider.DisplayName;
        PackageFullName = packageFullName;
        Name = ComputeSystem.DisplayName;
        AssociatedProviderId = ComputeSystem.AssociatedProviderId!;
        ComputeSystemId = ComputeSystem.Id!;
        _removalAction = removalAction;

        if (!string.IsNullOrEmpty(ComputeSystem.SupplementalDisplayName))
        {
            AlternativeName = new string("(" + ComputeSystem.SupplementalDisplayName + ")");
        }

        HeaderImage = CardProperty.ConvertMsResourceToIcon(provider.Icon, packageFullName);
        ComputeSystem.StateChanged += _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged += OnComputeSystemStateChanged;

        _stringResource = new StringResource("DevHome.Environments.pri", "DevHome.Environments/Resources");
    }

    public async Task InitializeCardDataAsync()
    {
        await InitializeStateAsync();
        await SetBodyImageAsync();
        await SetPropertiesAsync();
        await InitializeOperationDataAsync();
    }

    private async Task InitializeOperationDataAsync()
    {
        RegisterForAllOperationMessages(DataExtractor.FillDotButtonOperations(ComputeSystem!, _windowEx), DataExtractor.FillLaunchButtonOperations(ComputeSystem!));

        foreach (var operation in await DataExtractor.FillDotButtonPinOperationsAsync(ComputeSystem!))
        {
            DotOperations!.Add(operation);
        }
    }

    private async Task InitializeStateAsync()
    {
        var result = await ComputeSystem!.GetStateAsync();
        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"Failed to get state for {ComputeSystem.DisplayName} due to {result.Result.DiagnosticText}");
        }

        State = result.State;
        StateColor = ComputeSystemHelpers.GetColorBasedOnState(State);

        SetupOperationProgressBasedOnState();
    }

    private async Task SetBodyImageAsync()
    {
        BodyImage = await ComputeSystemHelpers.GetBitmapImageAsync(ComputeSystem!);
    }

    private async Task SetPropertiesAsync()
    {
        foreach (var property in await ComputeSystemHelpers.GetComputeSystemPropertiesAsync(ComputeSystem!, PackageFullName))
        {
            Properties.Add(property);
        }
    }

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState newState)
    {
        _windowEx.DispatcherQueue.EnqueueAsync(async () =>
        {
            if (sender.Id == ComputeSystem!.Id)
            {
                // The supported operations for a compute system can change based on the current state of the compute system.
                // So we need to rebuild the dot and launch operations that appear in the UI based on the current
                // supported operations of the compute system. InitializeOperationDataAsync will take care of this for us, by using
                // the DataExtractor helper.
                await InitializeOperationDataAsync();
                State = newState;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(newState);
                SetupOperationProgressBasedOnState();
            }
        });
    }

    public void RemoveStateChangedHandler()
    {
        ComputeSystem!.StateChanged -= _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged -= OnComputeSystemStateChanged;

        // Unregister from all operation messages
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [RelayCommand]
    public void LaunchAction()
    {
        LastConnected = DateTime.Now;

        // We'll need to disable the card UI while the operation is in progress and handle failures.
        Task.Run(async () =>
        {
            _windowEx.DispatcherQueue.TryEnqueue(() =>
            {
                UiMessageToDisplay = _stringResource.GetLocalized("LaunchingEnvironmentText");
                IsOperationInProgress = true;
            });

            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_Launch_Event",
                LogLevel.Critical,
                new EnvironmentLaunchUserEvent(ComputeSystem!.AssociatedProviderId, EnvironmentsTelemetryStatus.Started));

            var operationResult = await ComputeSystem!.ConnectAsync(string.Empty);

            var completionStatus = EnvironmentsTelemetryStatus.Succeeded;
            var completionMessage = _stringResource.GetLocalized("LaunchingEnvironmentSuccessText");
            var operationFailed = (operationResult == null) || (operationResult.Result.Status == ProviderOperationStatus.Failure);

            if (operationFailed)
            {
                completionStatus = EnvironmentsTelemetryStatus.Failed;
                LogFailure(operationResult);

                var messageWhenNull = _stringResource.GetLocalized("LaunchingEnvironmentFailedUnKnownReasonText");
                completionMessage =
                    (operationResult != null) ? _stringResource.GetLocalized("LaunchingEnvironmentFailedText", operationResult.Result.DisplayMessage) : messageWhenNull;
            }

            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_Launch_Event",
                LogLevel.Critical,
                new EnvironmentLaunchUserEvent(ComputeSystem!.AssociatedProviderId, completionStatus));

            _windowEx.DispatcherQueue.TryEnqueue(() =>
            {
                UiMessageToDisplay = completionMessage;
                IsOperationInProgress = false;
                UiMessageToDisplay = operationFailed ? UiMessageToDisplay : string.Empty;
            });
        });
    }

    private void RemoveComputeSystem()
    {
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            _log.Information($"Removing Compute system with Name: {ComputeSystem!.DisplayName} from UI");
            _removalAction(this);
            RemoveStateChangedHandler();
        });
    }

    private void LogFailure(ComputeSystemOperationResult? computeSystemOperationResult)
    {
        if (computeSystemOperationResult == null)
        {
            _log.Error($"Launch operation failed for {ComputeSystem}. The ComputeSystemOperationResult was null");
        }
        else
        {
            _log.Error(computeSystemOperationResult.Result.ExtendedError, $"Launch operation failed for {ComputeSystem} error: {computeSystemOperationResult.Result.DiagnosticText}");
        }
    }

    /// <summary>
    /// Implements the Receive method from the IRecipient<ComputeSystemOperationStartedMessage> interface. When this message
    /// is received we fire the first telemetry event to capture which operation and provider is starting.
    /// </summary>
    /// <param name="message">The object that holds the data needed to capture the operationInvoked telemetry data</param>
    public void Receive(ComputeSystemOperationStartedMessage message)
    {
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            var data = message.Value;
            IsOperationInProgress = true;

            _log.Information($"operation '{data.ComputeSystemOperation}' starting for Compute System: {Name} at {DateTime.Now}");
            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_OperationInvoked_Event",
                LogLevel.Measure,
                new EnvironmentOperationUserEvent(data.TelemetryStatus, data.ComputeSystemOperation, ComputeSystem!.AssociatedProviderId, data.AdditionalContext, data.ActivityId));
        });
    }

    /// <summary>
    /// Implements the Receive method from the IRecipient<ComputeSystemOperationCompletedMessage> interface. When this message
    /// is received the operation is completed and we can log the result of the operation.
    /// </summary>
    /// <param name="message">The object that holds the data needed to capture the operationInvoked telemetry data</param>
    public void Receive(ComputeSystemOperationCompletedMessage message)
    {
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            var data = message.Value;
            _log.Information($"operation '{data.ComputeSystemOperation}' completed for Compute System: {Name} at {DateTime.Now}");

            var completionStatus = EnvironmentsTelemetryStatus.Succeeded;

            if ((data.OperationResult == null) || (data.OperationResult.Result.Status == ProviderOperationStatus.Failure))
            {
                completionStatus = EnvironmentsTelemetryStatus.Failed;
                LogFailure(data.OperationResult);
            }

            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_OperationInvoked_Event",
                LogLevel.Measure,
                new EnvironmentOperationUserEvent(completionStatus, data.ComputeSystemOperation, ComputeSystem!.AssociatedProviderId, data.AdditionalContext, data.ActivityId));
        });
    }

    /// <summary>
    /// Register the ViewModel to receive messages for the start and completion of operations from all view models within the
    /// DotOperation and LaunchOperation lists. When there is an operation this ViewModel will receive the started and
    /// the completed messages.
    /// </summary>
    private void RegisterForAllOperationMessages(List<OperationsViewModel> dotOperations, List<OperationsViewModel> launchOperations)
    {
        _log.Information($"Registering ComputeSystemViewModel '{Name}' from provider '{ProviderDisplayName}' with WeakReferenceMessenger at {DateTime.Now}");

        // Unregister from all operation messages
        WeakReferenceMessenger.Default.UnregisterAll(this);
        LaunchOperations.Clear();
        DotOperations!.Clear();

        foreach (var dotOperation in dotOperations)
        {
            DotOperations.Add(dotOperation);
            WeakReferenceMessenger.Default.Register<ComputeSystemOperationStartedMessage, OperationsViewModel>(this, dotOperation);
            WeakReferenceMessenger.Default.Register<ComputeSystemOperationCompletedMessage, OperationsViewModel>(this, dotOperation);
        }

        foreach (var launchOperation in launchOperations)
        {
            LaunchOperations.Add(launchOperation);
            WeakReferenceMessenger.Default.Register<ComputeSystemOperationStartedMessage, OperationsViewModel>(this, launchOperation);
            WeakReferenceMessenger.Default.Register<ComputeSystemOperationCompletedMessage, OperationsViewModel>(this, launchOperation);
        }
    }

    private bool IsComputeSystemStateTransitioning(ComputeSystemState state)
    {
        switch (state)
        {
            case ComputeSystemState.Starting:
            case ComputeSystemState.Saving:
            case ComputeSystemState.Stopping:
            case ComputeSystemState.Pausing:
            case ComputeSystemState.Restarting:
            case ComputeSystemState.Creating:
            case ComputeSystemState.Deleting:
                return true;
            default:
                return false;
        }
    }

    private void SetupOperationProgressBasedOnState()
    {
        if (IsComputeSystemStateTransitioning(State))
        {
            IsOperationInProgress = true;
        }
        else
        {
            IsOperationInProgress = false;
        }

        if ((State != ComputeSystemState.Creating) && (State != ComputeSystemState.Deleting))
        {
            ShouldShowLaunchOperation = true;
        }

        if (State == ComputeSystemState.Deleted)
        {
            RemoveComputeSystem();
        }
    }
}
