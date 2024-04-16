// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Environments.Helpers;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinUIEx;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// View model for a compute system. Each 'card' in the UI represents a compute system.
/// Contains an instance of the compute system object as well.
/// </summary>
public partial class ComputeSystemViewModel : ComputeSystemCardBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemViewModel));

    private readonly StringResource _stringResource;

    private readonly WindowEx _windowEx;

    private readonly IComputeSystemManager _computeSystemManager;

    // Launch button operations
    public ObservableCollection<OperationsViewModel> LaunchOperations { get; set; }

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
        ShouldShowLaunchOperation = true;

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
    }

    public async Task InitializeCardDataAsync()
    {
        await InitializeStateAsync();
        await SetBodyImageAsync();
        await SetPropertiesAsync();
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
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            if (sender.Id == ComputeSystem!.Id)
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
        ComputeSystem!.StateChanged -= _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged -= OnComputeSystemStateChanged;
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
