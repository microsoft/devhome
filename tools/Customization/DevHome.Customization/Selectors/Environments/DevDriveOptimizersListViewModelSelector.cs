// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.Models.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Selectors.Environments;

/// <summary>
/// Data template class for selecting the list view template for the dev drive insights page.
/// </summary>
public class DevDriveOptimizersListViewModelSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template when dev drives are loaded into the DevDriveOptimizersListViewModel's ownerlist.
    /// </summary>
    public DataTemplate DevDriveOptimizersListViewModelLoadedTemplate { get; set; } = new();

    /// <summary>
    /// Gets or sets the template when there is a non-interactable error that occured when loading the DevDriveOptimizersListViewModel's list
    /// </summary>
    public DataTemplate DevDriveOptimizersListViewModelLoadingErrorTemplate { get; set; } = new();

    /// <summary>
    /// Gets or sets the template when there are no DevDriveOptimizerCardViewModels available in a DevDriveOptimizersListViewModel's list.
    /// </summary>
    public DataTemplate NoDevDriveOptimizerCardViewModelsAvailableTemplate { get; set; } = new();

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item);
    }

    /// <summary>
    /// Resolves the data template based on the if the DevDriveOptimizersListViewModel currently containers any DevDriveOptimizerWrappers.
    /// </summary>
    /// <param name="item">The DevDriveOptimizersListViewModel object</param>
    private DataTemplate ResolveDataTemplate(object item)
    {
        if (item is DevDriveOptimizersListViewModel listViewModel)
        {
            if (listViewModel.DevDriveOptimizerCardAdvancedCollectionView.Count > 0)
            {
                return DevDriveOptimizersListViewModelLoadedTemplate;
            }

            if (listViewModel.DevDriveOptimizerCardAdvancedCollectionView.Count == 0)
            {
                return NoDevDriveOptimizerCardViewModelsAvailableTemplate;
            }
        }

        return DevDriveOptimizersListViewModelLoadingErrorTemplate;
    }
}
