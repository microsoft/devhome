// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.Models.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Selectors.Environments;

/// <summary>
/// Data template class for selecting the list view template for the dev drive insights page.
/// </summary>
public class DevDriveOptimizedListViewModelSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template when dev drives are loaded into the DevDriveOptimizedListViewModel's ownerlist.
    /// </summary>
    public DataTemplate DevDriveOptimizedListViewModelLoadedTemplate { get; set; } = new();

    /// <summary>
    /// Gets or sets the template when there is a non-interactable error that occured when loading the DevDriveOptimizedListViewModel's list
    /// </summary>
    public DataTemplate DevDriveOptimizedListViewModelLoadingErrorTemplate { get; set; } = new();

    /// <summary>
    /// Gets or sets the template when there are no DevDriveOptimizedCardViewModels available in a DevDriveOptimizedListViewModel's list.
    /// </summary>
    public DataTemplate NoDevDriveOptimizedCardViewModelsAvailableTemplate { get; set; } = new();

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item);
    }

    /// <summary>
    /// Resolves the data template based on the if the DevDriveOptimizedListViewModel currently containers any DevDriveOptimizedWrappers.
    /// </summary>
    /// <param name="item">The DevDriveOptimizedListViewModel object</param>
    private DataTemplate ResolveDataTemplate(object item)
    {
        if (item is DevDriveOptimizedListViewModel listViewModel)
        {
            if (listViewModel.DevDriveOptimizedCardAdvancedCollectionView.Count > 0)
            {
                return DevDriveOptimizedListViewModelLoadedTemplate;
            }

            if (listViewModel.DevDriveOptimizedCardAdvancedCollectionView.Count == 0)
            {
                return NoDevDriveOptimizedCardViewModelsAvailableTemplate;
            }
        }

        return DevDriveOptimizedListViewModelLoadingErrorTemplate;
    }
}
