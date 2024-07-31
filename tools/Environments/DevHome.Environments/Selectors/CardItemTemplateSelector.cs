// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Environments.Selectors;

public class CardItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CreateComputeSystemOperationTemplate { get; set; }

    public DataTemplate? ComputeSystemTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return ResolveDataTemplate(item);
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return ResolveDataTemplate(item);
    }

    /// <summary>
    /// Resolves the data template based on the if the ComputeSystemsListViewModel currently containers any ComputeSystems.
    /// </summary>
    /// <param name="item">The ComputeSystemsListViewModel object</param>
    private DataTemplate? ResolveDataTemplate(object item)
    {
        if (item is CreateComputeSystemOperationViewModel)
        {
            return CreateComputeSystemOperationTemplate;
        }
        else
        {
            return ComputeSystemTemplate;
        }
    }
}
