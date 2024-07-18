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

    public bool IsOperationInProgress { get; set; }

    // Launch button operations
    public ObservableCollection<OperationsViewModel> LaunchOperations { get; set; }

    public ObservableCollection<CardProperty> Properties { get; set; } = new();

    public string PackageFullName { get; set; }

    private readonly Func<ComputeSystemCardBase, bool> _removalAction;

    [ObservableProperty]
    private bool _shouldShowDotOperations;

    [ObservableProperty]
    private bool _shouldShowSplitButton;

    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeSystemViewModel"/> class.
    /// This class requires a 3-step initialization:
    /// 1. Create the instance of the class. Constructor saves the parameters, but doesn't make
    ///    any OOP calls to IComputeSystem or initialize UX data which requires UI thread.
    /// 2. Call <see cref="InitializeCardDataAsync"/> to fetch the compute system data from the extension and cache it in ComputeSystem property.
    ///    This can be done on any thread and in parallel with other compute systems.
    /// 3. Call <see cref="InitializeUXData"/> to initialize the UX controls with the data we fetched in step 2.
    /// This allows us to avoid heavy calls on the UI thread and initialize data in parallel.
    /// </summary>
    public ComputeSystemViewModel(
        IComputeSystemManager manager,
        IComputeSystem system,
        ComputeSystemProvider provider,
        string packageFullName,
        WindowEx windowEx)
    {
        _windowEx = windowEx;
        _computeSystemManager = manager;

        ComputeSystem = new(system);
        ProviderDisplayName = provider.DisplayName;
        PackageFullName = packageFullName;
        Name = ComputeSystem.DisplayName;

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

    private async Task InitializeOperationDataAsync()
    {
        await _semaphoreSlimLock.WaitAsync();
        try
        {
            ShouldShowDotOperations = false;
            ShouldShowSplitButton = false;

            RegisterForAllOperationMessages(DataExtractor.FillDotButtonOperations(ComputeSystem, _mainWindow), DataExtractor.FillLaunchButtonOperations(ComputeSystem));

            _ = Task.Run(async () =>
            {
                var start = DateTime.Now;
                List<OperationsViewModel> validData = new();
                foreach (var data in await DataExtractor.FillDotButtonPinOperationsAsync(ComputeSystem))
                {
                    if ((!data.WasPinnedStatusSuccessful) || (data.ViewModel == null))
                    {
                        _log.Error($"Pinned status check failed: for '{Name}': {data?.PinnedStatusDisplayMessage}. DiagnosticText: {data?.PinnedStatusDiagnosticText}");
                        continue;
                    }

                    validData.Add(data.ViewModel);
                    WeakReferenceMessenger.Default.Register<ComputeSystemOperationStartedMessage, OperationsViewModel>(this, data.ViewModel);
                    WeakReferenceMessenger.Default.Register<ComputeSystemOperationCompletedMessage, OperationsViewModel>(this, data.ViewModel);
                }

                _log.Information($"Registering pin operations for {Name} in background took {DateTime.Now - start}");

                // Add valid data to the DotOperations collection
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var data in validData)
                    {
                        DotOperations.Add(data);
                    }

                    // Only show dot operations when there are items in the list.
                    ShouldShowDotOperations = DotOperations.Count > 0;

                    // Only show Launch split button with operations when there are items in the list.
                    ShouldShowSplitButton = LaunchOperations.Count > 0;
                });
            });

            SetPropertiesAsync();
        }
        finally
        {
            _semaphoreSlimLock.Release();
        }
    }

    private async Task RefreshOperationDataAsync()
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
            IsOperationInProgress = true;

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

            IsOperationInProgress = false;
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
