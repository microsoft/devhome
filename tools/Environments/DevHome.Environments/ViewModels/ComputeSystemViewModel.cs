// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
<<<<<<< Updated upstream
using DevHome.Environments.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
=======
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
>>>>>>> Stashed changes

namespace DevHome.Environments.ViewModels;

/// <summary>
/// View model for a compute system. Each 'card' in the UI represents a compute system.
/// Contains an instance of the compute system object as well.
/// </summary>
<<<<<<< Updated upstream
public partial class ComputeSystemViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemViewModel));

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    public string Name => ComputeSystem.DisplayName;

    private readonly IComputeSystemManager _computeSystemManager;

    public ComputeSystem ComputeSystem { get; }

    public string AlternativeName { get; } = string.Empty;

    public DateTime LastConnected { get; set; } = DateTime.Now;

    public string Type { get; }

    public bool IsOperationInProgress { get; set; }

=======
public partial class ComputeSystemViewModel : ComputeSystemCardBase, IRecipient<ComputeSystemOperationStartedMessage>, IRecipient<ComputeSystemOperationCompletedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemViewModel));

    private readonly StringResource _stringResource;

    private readonly WindowEx _windowEx;

    private readonly IComputeSystemManager _computeSystemManager;

>>>>>>> Stashed changes
    // Launch button operations
    public ObservableCollection<OperationsViewModel> LaunchOperations { get; set; }

    // Dot button operations
    public ObservableCollection<OperationsViewModel> DotOperations { get; set; }

    public ObservableCollection<CardProperty> Properties { get; set; } = new();

    [ObservableProperty]
    private ComputeSystemState _state;

    [ObservableProperty]
    private CardStateColor _stateColor;

    public BitmapImage? HeaderImage { get; set; } = new();

    public BitmapImage? BodyImage { get; set; } = new();

    public string PackageFullName { get; set; }

<<<<<<< Updated upstream
    public ComputeSystemViewModel(IComputeSystemManager manager, IComputeSystem system, ComputeSystemProvider provider, string packageFullName)
=======
    private readonly Func<ComputeSystemCardBase, bool> _removalAction;

    public ComputeSystemViewModel(
        IComputeSystemManager manager,
        IComputeSystem system,
        ComputeSystemProvider provider,
        Func<ComputeSystemCardBase, bool> removalAction,
        string packageFullName,
        WindowEx windowEx)
>>>>>>> Stashed changes
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _computeSystemManager = manager;

        ComputeSystem = new(system);
        Type = provider.DisplayName;
        PackageFullName = packageFullName;
<<<<<<< Updated upstream
=======
        Name = ComputeSystem.DisplayName;
        AssociatedProviderId = ComputeSystem.AssociatedProviderId!;
        ComputeSystemId = ComputeSystem.Id!;
        _removalAction = removalAction;
        ShouldShowLaunchOperation = true;
>>>>>>> Stashed changes

        if (!string.IsNullOrEmpty(ComputeSystem.SupplementalDisplayName))
        {
            AlternativeName = new string("(" + ComputeSystem.SupplementalDisplayName + ")");
        }

        LaunchOperations = new ObservableCollection<OperationsViewModel>(DataExtractor.FillLaunchButtonOperations(ComputeSystem));
        DotOperations = new ObservableCollection<OperationsViewModel>(DataExtractor.FillDotButtonOperations(ComputeSystem));
        HeaderImage = CardProperty.ConvertMsResourceToIcon(provider.Icon, packageFullName);
        ComputeSystem.StateChanged += _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged += OnComputeSystemStateChanged;

        _stringResource = new StringResource("DevHome.Environments.pri", "DevHome.Environments/Resources");
        RegisterForAllOperationMessages();
    }

    public async Task InitializeCardDataAsync()
    {
        await InitializeStateAsync();
        await SetBodyImageAsync();
        await SetPropertiesAsync();
        await InitializePinDataAsync();
    }

    private async Task InitializePinDataAsync()
    {
        // We know ComputeSystem and DotOperations are initialized in the constructor so it's safe to use
        var operations = new ObservableCollection<OperationsViewModel>(await DataExtractor.FillDotButtonPinOperationsAsync(ComputeSystem!));
        foreach (var operation in operations)
        {
            DotOperations!.Add(operation);
        }
    }

    private async Task InitializeStateAsync()
    {
        var result = await ComputeSystem.GetStateAsync();
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
        BodyImage = await ComputeSystemHelpers.GetBitmapImageAsync(ComputeSystem);
    }

    private async Task SetPropertiesAsync()
    {
        foreach (var property in await ComputeSystemHelpers.GetComputeSystemPropertiesAsync(ComputeSystem, PackageFullName))
        {
            Properties.Add(property);
        }
    }

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState newState)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (sender.Id == ComputeSystem.Id)
            {
                UpdateOperationsPostCreation(State, newState);
                State = newState;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(newState);
                SetupOperationProgressBasedOnState();
            }
        });
    }

    public void RemoveStateChangedHandler()
    {
        ComputeSystem.StateChanged -= _computeSystemManager.OnComputeSystemStateChanged;
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
<<<<<<< Updated upstream
            await ComputeSystem.ConnectAsync(string.Empty);
=======

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

>>>>>>> Stashed changes
            IsOperationInProgress = false;
        });
    }

    /// <summary>
    /// Register the ViewModel to receive messages for the start and completion of operations from all view models within the
    /// DotOperation and LaunchOperation lists. When there is an operation this ViewModel will receive the started and
    /// the completed messages.
    /// </summary>
    private void RegisterForAllOperationMessages()
    {
        _log.Information($"Registering ComputeSystemViewModel '{Name}' from provider '{ProviderDisplayName}' with WeakReferenceMessenger at {DateTime.Now}");

        foreach (var dotOperation in DotOperations!)
        {
            WeakReferenceMessenger.Default.Register<ComputeSystemOperationStartedMessage, OperationsViewModel>(this, dotOperation);
            WeakReferenceMessenger.Default.Register<ComputeSystemOperationCompletedMessage, OperationsViewModel>(this, dotOperation);
        }

        foreach (var launchOperation in LaunchOperations!)
        {
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

        if ((State != ComputeSystemState.Creating) || (State != ComputeSystemState.Deleting))
        {
            ShouldShowLaunchOperation = true;
        }

        if (State == ComputeSystemState.Deleted)
        {
            RemoveComputeSystem();
        }
    }

    private void UpdateOperationsPostCreation(ComputeSystemState previousState, ComputeSystemState newState)
    {
        // supported operations may have changed after creation, so we'll update them
        if ((previousState == ComputeSystemState.Creating) && (previousState != newState))
        {
            LaunchOperations.Clear();
            DotOperations!.Clear();

            foreach (var buttonOperation in DataExtractor.FillLaunchButtonOperations(ComputeSystem!))
            {
                LaunchOperations.Add(buttonOperation);
            }

            foreach (var dotOperation in DataExtractor.FillDotButtonOperations(ComputeSystem!))
            {
                LaunchOperations.Add(dotOperation);
            }
        }
    }
}
