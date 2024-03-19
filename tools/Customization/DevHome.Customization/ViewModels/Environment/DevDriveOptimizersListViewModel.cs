// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using DevHome.Customization.ViewModels.Environments;
using DevHome.SetupFlow.Models;

namespace DevHome.Customization.Models.Environments;

/// <summary>
/// The view model for the list of dev drives retrieved from a single dev drive provider.
/// </summary>
public partial class DevDriveOptimizersListViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _selectedItem = null;

    public ObservableCollection<DevDriveOptimizerCardViewModel> DevDriveOptimizerCardCollection { get; private set; } = new();

    public AdvancedCollectionView DevDriveOptimizerCardAdvancedCollectionView { get; private set; }

    /// <summary>
    /// Gets the error text that will appear in the UI.
    /// </summary>
    public string ErrorText => string.Empty;

    public DevDriveOptimizersListViewModel()
    {
        // Create a new AdvancedCollectionView for the DevDriveCards collection.
        DevDriveOptimizerCardAdvancedCollectionView = new(DevDriveOptimizerCardCollection);
    }

    /// <summary>
    /// Filter the cards based on the text entered in the search box. Cards will be filtered by the DevDriveTitle.
    /// </summary>
    /// <param name="text">Text the user enters into the textbox</param>
    public void FilterDevDriveCards(string text)
    {
        DevDriveOptimizerCardAdvancedCollectionView.Filter = item =>
        {
            try
            {
                if (item is DevDriveOptimizerCardViewModel card)
                {
                    return string.IsNullOrEmpty(text);
                }

                return false;
            }
            catch (Exception /*ex*/)
            {
                // Log.Logger.ReportError(Log.Component.DevDriveOptimizersListViewModel, $"Failed to filter Compute system cards. Error: {ex.Message}");
            }

            return true;
        };

        DevDriveOptimizerCardAdvancedCollectionView.RefreshFilter();
    }

    /// <summary>
    /// Update subscriber with the dev drive wrapper that is currently selected in the UI.
    /// </summary>
    /// <param name="viewModel">Environments card selected by the user.</param>
    [RelayCommand]
    public void ContainerSelectionChanged(DevDriveCardViewModel viewModel)
    {
        if (viewModel == null)
        {
            return;
        }

        SelectedItem = viewModel;
    }
}
