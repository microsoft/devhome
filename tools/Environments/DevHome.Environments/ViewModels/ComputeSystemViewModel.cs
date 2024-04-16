// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
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

        if (State == ComputeSystemState.Creating || State == ComputeSystemState.Deleting)
        {
            IsOperationInProgress = true;
            ShouldShowLaunchOperation = false;
        }
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

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state)
    {
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            if (sender.Id == ComputeSystem!.Id)
            {
                State = state;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(state);

                if (State != ComputeSystemState.Creating || State != ComputeSystemState.Deleting)
                {
                    ShouldShowLaunchOperation = true;
                }

                if (State == ComputeSystemState.Deleted)
                {
                    RemoveComputeSystem();
                }
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
            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_Launch_Event",
                LogLevel.Critical,
                new EnvironmentLaunchUserEvent(ComputeSystem!.AssociatedProviderId, EnvironmentsTelemetryStatus.Started));

            var operationResult = await ComputeSystem!.ConnectAsync(string.Empty);

            var completionStatus = EnvironmentsTelemetryStatus.Succeeded;

            if ((operationResult == null) || (operationResult.Result.Status == ProviderOperationStatus.Failure))
            {
                completionStatus = EnvironmentsTelemetryStatus.Failed;
                LogFailure(operationResult);
            }

            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_Launch_Event",
                LogLevel.Critical,
                new EnvironmentLaunchUserEvent(ComputeSystem!.AssociatedProviderId, completionStatus));

            await ComputeSystem!.ConnectAsync(string.Empty);
        });
    }

    private void RemoveComputeSystem()
    {
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
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
}
