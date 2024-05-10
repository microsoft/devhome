// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a compute system on the setup target page.
/// </summary>
public partial class ComputeSystemCardViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemCardViewModel));

    private readonly WindowEx _windowEx;

    private readonly IComputeSystemManager _computeSystemManager;

    private const int _maxCardProperties = 6;

    public ComputeSystemCache ComputeSystem { get; private set; }

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

    public ComputeSystemCardViewModel(ComputeSystemCache computeSystem, IComputeSystemManager manager, WindowEx windowEx)
    {
        _windowEx = windowEx;
        _computeSystemManager = manager;
        ComputeSystemTitle = computeSystem.DisplayName.Value;
        ComputeSystem = computeSystem;
        ComputeSystem.StateChanged += _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged += OnComputeSystemStateChanged;
        AccessibilityName = new Lazy<string>(BuildAutomationName);
    }

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state)
    {
        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            if (sender.Id == ComputeSystem.Id.Value)
            {
                CardState = state;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(state);
            }
        });
    }

    public async Task<ComputeSystemState> GetCardStateAsync()
    {
        var result = await ComputeSystem.GetStateAsync();

        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"Failed to get state for compute system {ComputeSystem.DisplayName} from provider {ComputeSystem.AssociatedProviderId}. Error: {result.Result.DiagnosticText}");
        }

        StateColor = ComputeSystemHelpers.GetColorBasedOnState(result.State);
        return result.State;
    }

    public void RemoveComputeSystemStateChangedHandler()
    {
        ComputeSystem.StateChanged -= _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged -= OnComputeSystemStateChanged;
    }

    private string BuildAutomationName()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(CultureInfo.CurrentCulture, $"{ComputeSystemTitle}");
        stringBuilder.AppendLine(CultureInfo.CurrentCulture, $"{CardState}");

        foreach (var property in ComputeSystemProperties)
        {
            stringBuilder.AppendLine(CultureInfo.CurrentCulture, $"{property}");
        }

        return stringBuilder.ToString();
    }
}
