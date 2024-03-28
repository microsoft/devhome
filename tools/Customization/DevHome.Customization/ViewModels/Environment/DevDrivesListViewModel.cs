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
/// The view model for the list of dev drives on the box.
/// </summary>
public partial class DevDrivesListViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _selectedItem = null;

    public ObservableCollection<DevDriveCardViewModel> DevDriveCardCollection { get; private set; } = new();

    public AdvancedCollectionView DevDriveCardAdvancedCollectionView { get; private set; }

    /// <summary>
    /// Gets the error text that will appear in the UI.
    /// </summary>
    public string ErrorText => string.Empty;

    public DevDrivesListViewModel()
    {
        // Create a new AdvancedCollectionView for the DevDriveCards collection.
        DevDriveCardAdvancedCollectionView = new(DevDriveCardCollection);
    }

    /// <summary>
    /// Filter the cards based on the text entered in the search box. Cards will be filtered by the DevDriveTitle.
    /// </summary>
    /// <param name="text">Text the user enters into the textbox</param>
    public void FilterDevDriveCards(string text)
    {
        DevDriveCardAdvancedCollectionView.Filter = item =>
        {
            try
            {
                if (item is DevDriveCardViewModel card)
                {
                    return string.IsNullOrEmpty(text);
                }

                return false;
            }
            catch (Exception /*ex*/)
            {
                // Log.Logger.ReportError(Log.Component.DevDrivesListViewModel, $"Failed to filter Compute system cards. Error: {ex.Message}");
            }

            return true;
        };

        DevDriveCardAdvancedCollectionView.RefreshFilter();
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
