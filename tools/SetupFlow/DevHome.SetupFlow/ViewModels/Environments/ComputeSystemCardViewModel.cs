// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a compute system on the setup target page.
/// </summary>
public partial class ComputeSystemCardViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemCardViewModel));

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly string _packageFullName;

    private readonly object _lock = new();

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

    private readonly StringResource _stringResourceCommon = new("DevHome.Common.pri", "DevHome.Common/Resources");

    public ObservableCollection<CardProperty> ComputeSystemProperties { get; set; }

    public ComputeSystemCardViewModel(ComputeSystemCache computeSystem, IComputeSystemManager manager, DispatcherQueue dispatcherQueue, string packageFullName)
    {
        _dispatcherQueue = dispatcherQueue;
        _computeSystemManager = manager;
        ComputeSystemTitle = computeSystem.DisplayName.Value;
        ComputeSystem = computeSystem;
        ComputeSystem.StateChanged += _computeSystemManager.OnComputeSystemStateChanged;
        _computeSystemManager.ComputeSystemStateChanged += OnComputeSystemStateChanged;
        AccessibilityName = new Lazy<string>(BuildAutomationName);
        _packageFullName = packageFullName;
    }

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state)
    {
        _dispatcherQueue.EnqueueAsync(async () =>
        {
            if (sender.Id == ComputeSystem.Id.Value &&
                sender.AssociatedProviderId.Equals(ComputeSystem.AssociatedProviderId.Value, StringComparison.OrdinalIgnoreCase))
            {
                CardState = state;
                StateColor = ComputeSystemHelpers.GetColorBasedOnState(state);
                ComputeSystem.ResetComputeSystemProperties();
                await RefreshOperationDataAsync();
            }
        });
    }

    private async Task RefreshOperationDataAsync()
    {
        ComputeSystem.ResetComputeSystemProperties();
        await UpdatePropertiesAsync();
    }

    private async Task UpdatePropertiesAsync()
    {
        var properties = await ComputeSystemHelpers.GetComputeSystemCardPropertiesAsync(ComputeSystem, _packageFullName);
        lock (_lock)
        {
            if (!ComputeSystemHelpers.RemoveAllItemsAndReplace(ComputeSystemProperties, properties))
            {
                ComputeSystemProperties = new(properties);
            }
        }
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
        var state = _stringResourceCommon.GetLocalized($"ComputeSystem{CardState}");
        stringBuilder.AppendLine(string.Format(CultureInfo.CurrentCulture, "{0}", state));

        foreach (var property in ComputeSystemProperties)
        {
            stringBuilder.AppendLine(CultureInfo.CurrentCulture, $"{property}");
        }

        return stringBuilder.ToString();
    }
}
