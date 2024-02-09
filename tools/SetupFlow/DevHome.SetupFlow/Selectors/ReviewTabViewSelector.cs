// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Selectors;

/// <summary>
/// Data template selector class for rendering the current active tab in the review page.
/// For example, if the DevDriveReviewViewModel is currently bound to the
/// content control, then the DevDriveReviewView will render.
/// </summary>
public class ReviewTabViewSelector : DataTemplateSelector
{
    public DataTemplate DevDriveTabTemplate
    {
        get; set;
    }

    public DataTemplate RepoConfigTabTemplate
    {
        get; set;
    }

    public DataTemplate AppManagementTabTemplate
    {
        get; set;
    }

    public DataTemplate SetupTargetTabTemplate
    {
        get; set;
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item, () => base.SelectTemplateCore(item));
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item, () => base.SelectTemplateCore(item, container));
    }

    /// <summary>
    /// Resolve the data template for the given object type.
    /// </summary>
    /// <param name="item">Selected item.</param>
    /// <param name="defaultDataTemplate">Default data template function.</param>
    /// <returns>Data template or default data template if no corresponding data template was found.</returns>
    private DataTemplate ResolveDataTemplate(object item, Func<DataTemplate> defaultDataTemplate)
    {
        return item switch
        {
            DevDriveReviewViewModel => DevDriveTabTemplate,
            RepoConfigReviewViewModel => RepoConfigTabTemplate,
            AppManagementReviewViewModel => AppManagementTabTemplate,
            SetupTargetReviewViewModel => SetupTargetTabTemplate,
            _ => defaultDataTemplate(),
        };
    }
}
