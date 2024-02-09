// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Environments.Models;
using DevHome.Environments.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// View model for a compute system. Each 'card' in the UI represents a compute system.
/// Contains an instance of the compute system object as well.
/// </summary>
public partial class ComputeSystemViewModel : ObservableObject
{
    public IComputeSystem ComputeSystem { get; }

    public string Name => ComputeSystem.Name;

    public string AlternativeName { get; }

    public string Type { get; }

    // Launch button operations
    public ObservableCollection<OperationsViewModel> LaunchOperations { get; set; }

    // Dot button operations
    public ObservableCollection<OperationsViewModel> DotOperations { get; set; }

    public ObservableCollection<ICardProperty>? Properties { get; set; }

    public ComputeSystemState State { get; set; }

    public CardStateColor StateColor { get; private set; }

    public BitmapImage HeaderImage { get; set; }

    public BitmapImage BodyImage { get; set; }

    public ComputeSystemViewModel(IComputeSystem system, string displayName)
    {
        ComputeSystem = system;
        Type = displayName;
        AlternativeName = new string("(" + ComputeSystem.AlternativeDisplayName + ")");

        LaunchOperations = new ObservableCollection<OperationsViewModel>(DataExtractor.FillLaunchButtonOperations(system));
        DotOperations = new ObservableCollection<OperationsViewModel>(DataExtractor.FillDotButtonOperations(system));
        BodyImage = DataExtractor.GetCardBodyImage(system);
        Properties = new ObservableCollection<ICardProperty>(DataExtractor.FillPropertiesAsync(system));

        // ToDo: Remove this test value, and replace with the shared Card Property method once PFN is available
        HeaderImage = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/Preview/AppList.scale-100.png"), };

        InitializeState();
    }

    public async void InitializeState()
    {
        try
        {
            var result = await ComputeSystem.GetStateAsync(string.Empty);
            if (result.Result.Status == ProviderOperationStatus.Success)
            {
                State = result.State;
            }
            else
            {
                // ToDo: Log error
                State = ComputeSystemState.Unknown;
            }
        }
        catch (Exception e)
        {
            // ToDo: Log error & change test value
            Debug.WriteLine(e);
            State = ComputeSystemState.Running;
            StateColor = CardStateColor.Success;
        }
    }

    [RelayCommand]
    public async Task LaunchAction()
        => await ComputeSystem.ConnectAsync(string.Empty);
}
