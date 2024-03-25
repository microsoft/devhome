// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Customization.Models.Environments;

/// <summary>
/// The view model for the list of dev drives retrieved from a single dev drive provider.
/// </summary>
public partial class DevDriveOptimizedListViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _selectedItem = null;

    public ObservableCollection<DevDriveOptimizedCardViewModel> DevDriveOptimizedCardCollection { get; private set; } = new();

    public AdvancedCollectionView DevDriveOptimizedCardAdvancedCollectionView { get; private set; }

    /// <summary>
    /// Gets the error text that will appear in the UI.
    /// </summary>
    public string ErrorText => string.Empty;

    public DevDriveOptimizedListViewModel()
    {
        // Create a new AdvancedCollectionView for the DevDriveCards collection.
        DevDriveOptimizedCardAdvancedCollectionView = new(DevDriveOptimizedCardCollection);
    }

    /// <summary>
    /// Filter the cards based on the text entered in the search box. Cards will be filtered by the DevDriveTitle.
    /// </summary>
    /// <param name="text">Text the user enters into the textbox</param>
    public void FilterDevDriveCards(string text)
    {
        DevDriveOptimizedCardAdvancedCollectionView.Filter = item =>
        {
            try
            {
                if (item is DevDriveOptimizedCardViewModel card)
                {
                    return string.IsNullOrEmpty(text);
                }

                return false;
            }
            catch (Exception /*ex*/)
            {
                // Log.Logger.ReportError(Log.Component.DevDriveOptimizedListViewModel, $"Failed to filter Compute system cards. Error: {ex.Message}");
            }

            return true;
        };

        DevDriveOptimizedCardAdvancedCollectionView.RefreshFilter();
    }

    /// <summary>
    /// Update subscriber with the dev drive wrapper that is currently selected in the UI.
    /// </summary>
    /// <param name="viewModel">Environments card selected by the user.</param>
    [RelayCommand]
    public void ContainerSelectionChanged(DevDriveOptimizedCardViewModel viewModel)
    {
        if (viewModel == null)
        {
            return;
        }

        SelectedItem = viewModel;
    }
}
