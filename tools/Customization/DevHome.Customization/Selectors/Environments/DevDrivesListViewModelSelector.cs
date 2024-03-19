// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.Models.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Selectors.Environments;

/// <summary>
/// Data template class for selecting the list view template for the dev drive insights page.
/// </summary>
public class DevDrivesListViewModelSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template when dev drives are loaded into the DevDrivesListViewModel's ownerlist.
    /// </summary>
    public DataTemplate DevDrivesListViewModelLoadedTemplate { get; set; } = new();

    /// <summary>
    /// Gets or sets the template when there is a non-interactable error that occured when loading the DevDrivesListViewModel's list
    /// </summary>
    public DataTemplate DevDrivesListViewModelLoadingErrorTemplate { get; set; } = new();

    /// <summary>
    /// Gets or sets the template when there are no DevDriveCardViewModels available in a DevDrivesListViewModel's list.
    /// </summary>
    public DataTemplate NoDevDriveCardViewModelsAvailableTemplate { get; set; } = new();

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item);
    }

    /// <summary>
    /// Resolves the data template based on the if the DevDrivesListViewModel currently containers any DevDriveWrappers.
    /// </summary>
    /// <param name="item">The DevDrivesListViewModel object</param>
    private DataTemplate ResolveDataTemplate(object item)
    {
        if (item is DevDrivesListViewModel listViewModel)
        {
            if (listViewModel.DevDriveCardAdvancedCollectionView.Count > 0)
            {
                return DevDrivesListViewModelLoadedTemplate;
            }

            if (listViewModel.DevDriveCardAdvancedCollectionView.Count == 0)
            {
                return NoDevDriveCardViewModelsAvailableTemplate;
            }
        }

        return DevDrivesListViewModelLoadingErrorTemplate;
    }
}
