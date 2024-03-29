// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using DevHome.Customization.ViewModels.Environments;

namespace DevHome.Customization.Models.Environments;

/// <summary>
/// The view model for the list of dev drive optimizers.
/// </summary>
public partial class DevDriveOptimizersListViewModel : ObservableObject
{
    public ObservableCollection<DevDriveOptimizerCardViewModel> DevDriveOptimizerCardCollection { get; private set; } = new();

    public AdvancedCollectionView DevDriveOptimizerCardAdvancedCollectionView { get; private set; }

    public DevDriveOptimizersListViewModel()
    {
        // Create a new AdvancedCollectionView for the DevDriveCards collection.
        DevDriveOptimizerCardAdvancedCollectionView = new(DevDriveOptimizerCardCollection);
    }
}
