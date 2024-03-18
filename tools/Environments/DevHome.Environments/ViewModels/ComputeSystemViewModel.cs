// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Helpers;
using DevHome.Environments.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// View model for a compute system. Each 'card' in the UI represents a compute system.
/// Contains an instance of the compute system object as well.
/// </summary>
public partial class ComputeSystemViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    public string Name => ComputeSystem.DisplayName;

    private readonly IComputeSystemManager _computeSystemManager;

    public ComputeSystem ComputeSystem { get; }

    public string AlternativeName { get; } = string.Empty;

    public string Type { get; }

    public bool IsOperationInProgress { get; set; }

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

    public ComputeSystemViewModel(IComputeSystemManager manager, IComputeSystem system, ComputeSystemProvider provider, string packageFullName)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _computeSystemManager = manager;

        ComputeSystem = new(system);
        Type = provider.DisplayName;
        PackageFullName = packageFullName;

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
        var result = await ComputeSystem.GetStateAsync();
        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            Log.Logger()?.ReportError($"Failed to get state for {ComputeSystem.DisplayName} due to {result.Result.DiagnosticText}");
        }

        State = result.State;
        StateColor = ComputeSystemHelpers.GetColorBasedOnState(State);
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

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (sender.Id == ComputeSystem.Id)
            {
                State = state;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(state);
            }
        });
    }

    public void RemoveStateChangedHandler()
    {
        ComputeSystem.StateChanged -= _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged -= OnComputeSystemStateChanged;
    }

    [RelayCommand]
    public void LaunchAction()
    {
        // We'll need to disable the card UI while the operation is in progress and handle failures.
        Task.Run(async () =>
        {
            IsOperationInProgress = true;
            await ComputeSystem.ConnectAsync(string.Empty);
            IsOperationInProgress = false;
        });
    }
}
