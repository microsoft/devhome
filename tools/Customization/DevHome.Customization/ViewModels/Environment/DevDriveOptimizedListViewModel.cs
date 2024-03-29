// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Customization.Models.Environments;

/// <summary>
/// The view model for the list of optimized caches.
/// </summary>
public partial class DevDriveOptimizedListViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _selectedItem = null;

    public ObservableCollection<DevDriveOptimizedCardViewModel> DevDriveOptimizedCardCollection { get; private set; } = new();

    public AdvancedCollectionView DevDriveOptimizedCardAdvancedCollectionView { get; private set; }

    public DevDriveOptimizedListViewModel()
    {
        // Create a new AdvancedCollectionView for the DevDriveCards collection.
        DevDriveOptimizedCardAdvancedCollectionView = new(DevDriveOptimizedCardCollection);
    }
}
