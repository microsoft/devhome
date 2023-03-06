// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.DevVolume.ViewModels;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Review.Selectors;

/// <summary>
/// Data template selector class for rendering the current active tab in the review page.
/// For example, if the DevVolumeReviewViewModel is currently bound to the
/// content control, then the DevVolumeReviewView will render.
/// </summary>
public class ReviewTabViewSelector : DataTemplateSelector
{
    public DataTemplate DevVolumeTabTemplate
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
            DevVolumeReviewViewModel => DevVolumeTabTemplate,
            RepoConfigReviewViewModel => RepoConfigTabTemplate,
            AppManagementReviewViewModel => AppManagementTabTemplate,
            _ => defaultDataTemplate(),
        };
    }
}
