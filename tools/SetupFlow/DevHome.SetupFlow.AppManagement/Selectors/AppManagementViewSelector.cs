// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.AppManagement.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.AppManagement.Selectors;

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
    /// Gets or sets the search template displayed during a search operation
    /// </summary>
    public DataTemplate SearchTemplate { get; set; }

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
            _ => MainTemplate,
        };
    }
}
