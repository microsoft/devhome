// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Selectors;

/// <summary>
/// Data template class for selecting the application management page's current
/// view template.
/// </summary>
public class AppManagementViewSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the default main template
    /// </summary>
    public DataTemplate MainTemplate { get; set; }

    /// <summary>
    /// Gets or sets the shimmer search template displayed during a search operation
    /// </summary>
    public DataTemplate ShimmerSearchTemplate { get; set; }

    /// <summary>
    /// Gets or sets the search template displayed after a search operation is completed
    /// </summary>
    public DataTemplate SearchTemplate { get; set; }

    /// <summary>
    /// Gets or sets the search message template displayed when an error or a message is shown
    /// </summary>
    public DataTemplate SearchMessageTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item);
    }

    private DataTemplate ResolveDataTemplate(object item)
    {
        return item switch
        {
            SearchViewModel => SearchTemplate,
            ShimmerSearchViewModel => ShimmerSearchTemplate,
            SearchMessageViewModel => SearchMessageTemplate,
            _ => MainTemplate,
        };
    }
}
