// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.Common.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

using Dispatching = Microsoft.UI.Dispatching;

namespace DevHome.SetupFlow.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a compute system on the setup target page.
/// </summary>
public partial class ComputeSystemCardViewModel : ObservableObject
{
    private readonly Dispatching.DispatcherQueue _dispatcher;

    private readonly IComputeSystemManager _computeSystemManager;

    private const int _maxCardProperties = 6;

    public ComputeSystem ComputeSystemWrapper { get; private set; }

    public BitmapImage ComputeSystemImage { get; set; }

    public BitmapImage ComputeSystemProviderImage { get; set; }

    public string ComputeSystemProviderName { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _computeSystemTitle;

    [ObservableProperty]
    private string _computeSystemProviderDisplayName;

    [ObservableProperty]
    private ComputeSystemState _cardState;

    [ObservableProperty]
    private CardStateColor _stateColor;

    // This will be used for the accessibility name of the compute system card.
    [ObservableProperty]
    private Lazy<string> _accessibilityName;

    public List<CardProperty> ComputeSystemProperties { get; set; }

    // only display first 6 properties
    public ObservableCollection<CardProperty> ComputeSystemPropertiesForCardUI
    {
        get
        {
            var properties = new ObservableCollection<CardProperty>();
            for (var i = 0; i < Math.Min(ComputeSystemProperties.Count, _maxCardProperties); i++)
            {
                properties.Add(ComputeSystemProperties[i]);
            }

            return properties;
        }
    }

    public ComputeSystemCardViewModel(ComputeSystem computeSystem, IComputeSystemManager manager)
    {
        _dispatcher = Dispatching.DispatcherQueue.GetForCurrentThread();
        _computeSystemManager = manager;
        ComputeSystemTitle = computeSystem.DisplayName;
        ComputeSystemWrapper = computeSystem;
        ComputeSystemWrapper.StateChanged += _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged += OnComputeSystemStateChanged;
    }

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (sender.Id == ComputeSystemWrapper.Id)
            {
                CardState = state;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(state);
            }
        });
    }

    public async Task<ComputeSystemState> GetCardStateAsync()
    {
        var result = await ComputeSystemWrapper.GetStateAsync();

        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            Log.Logger.ReportError(Log.Component.ComputeSystemCardViewModel, $"Failed to get state for compute system {ComputeSystemWrapper.DisplayName} from provider {ComputeSystemWrapper.AssociatedProviderId}. Error: {result.Result.DiagnosticText}");
        }

        StateColor = ComputeSystemHelpers.GetColorBasedOnState(result.State);
        return result.State;
    }

    public void RemoveComputeSystemStateChangedHandler()
    {
        ComputeSystemWrapper.StateChanged -= _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged -= OnComputeSystemStateChanged;
    }
}
