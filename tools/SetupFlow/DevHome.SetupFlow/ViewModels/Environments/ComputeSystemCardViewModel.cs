// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using DevHome.SetupFlow.Common.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a compute system on the setup target page.
/// </summary>
public partial class ComputeSystemCardViewModel : ObservableObject
{
    private const int _maxCardProperties = 6;

    public ComputeSystem ComputeSystemWrapper { get; set; }

    public BitmapImage ComputeSystemImage { get; set; }

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

    public List<ICardProperty> ComputeSystemProperties { get; set; }

    // only display first 6 properties
    public ObservableCollection<ICardProperty> ComputeSystemPropertiesForCardUI
    {
        get
        {
            var properties = new ObservableCollection<ICardProperty>();
            for (var i = 0; i < Math.Min(ComputeSystemProperties.Count, _maxCardProperties); i++)
            {
                properties.Add(ComputeSystemProperties[i]);
            }

            return properties;
        }
    }

    public void OnComputeSystemStateChanged(object sender, ComputeSystemState state)
    {
        CardState = state;
        UpdateStateColor(state);
    }

    public async Task<ComputeSystemState> GetCardStateAsync()
    {
        var result = await ComputeSystemWrapper.GetStateAsync(string.Empty);

        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            Log.Logger.ReportError(Log.Component.ComputeSystemCardViewModel, $"Failed to get state for compute system {ComputeSystemWrapper.Name} from provider {ComputeSystemWrapper.AssociatedProviderId}. Error: {result.Result.DiagnosticText}");
        }

        UpdateStateColor(result.State);
        return result.State;
    }

    private void UpdateStateColor(ComputeSystemState state)
    {
        StateColor = state switch
        {
            ComputeSystemState.Running => CardStateColor.Success,
            ComputeSystemState.Stopped => CardStateColor.Neutral,
            _ => CardStateColor.Caution,
        };
    }
}
