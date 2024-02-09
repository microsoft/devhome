// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.SetupFlow.Models.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Selectors.Environments;

/// <summary>
/// Data template class for selecting the list view template for the setup target page.
/// </summary>
public class ComputeSystemsListViewModelSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template when compute systems are loaded into the ComputeSystemsListViewModel's ownerlist.
    /// </summary>
    public DataTemplate ComputeSystemsListViewModelLoadedTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template when there is a non-interactable error that occured when loading the ComputeSystemsListViewModel's list
    /// </summary>
    public DataTemplate ComputeSystemsListViewModelLoadingErrorTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template when there are no ComputeSystemCardViewModels available in a ComputeSystemsListViewModel's list.
    /// </summary>
    public DataTemplate NoComputeSystemCardViewModelsAvailableTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item);
    }

    /// <summary>
    /// Resolves the data template based on the if the ComputeSystemsListViewModel currently containers any ComputeSystemWrappers.
    /// </summary>
    /// <param name="item">The ComputeSystemsListViewModel object</param>
    private DataTemplate ResolveDataTemplate(object item)
    {
        if (item is ComputeSystemsListViewModel listViewModel)
        {
            if (listViewModel.CurrentResult.Result.Status == ProviderOperationStatus.Failure)
            {
                return ComputeSystemsListViewModelLoadingErrorTemplate;
            }

            if (listViewModel.ComputeSystemCardAdvancedCollectionView.Count > 0)
            {
                return ComputeSystemsListViewModelLoadedTemplate;
            }

            if (listViewModel.ComputeSystemCardAdvancedCollectionView.Count == 0)
            {
                return NoComputeSystemCardViewModelsAvailableTemplate;
            }
        }

        return ComputeSystemsListViewModelLoadingErrorTemplate;
    }
}
