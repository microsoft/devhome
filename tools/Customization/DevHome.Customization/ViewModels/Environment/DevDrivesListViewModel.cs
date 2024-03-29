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
}
