// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Selectors;

/// <summary>
/// Data template selector class for rendering the current active page in the
/// setup flow. For example, if the MainPageViewModel is currently bound to the
/// content control, then the MainPageView will render.
/// </summary>
public class SetupFlowViewSelector : DataTemplateSelector
{
    public DataTemplate MainPageTemplate { get; set; }

    public DataTemplate RepoConfigTemplate { get; set; }

    public DataTemplate SetupTargetTemplate { get; set; }

    public DataTemplate AppManagementTemplate { get; set; }

    public DataTemplate ReviewTemplate { get; set; }

    public DataTemplate LoadingTemplate { get; set; }

    public DataTemplate SummaryTemplate { get; set; }

    public DataTemplate ConfigurationFileTemplate { get; set; }

    public DataTemplate SelectEnvironmentsProviderTemplate { get; set; }

    public DataTemplate EnvironmentCreationOptionsTemplate { get; set; }

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
            MainPageViewModel => MainPageTemplate,
            RepoConfigViewModel => RepoConfigTemplate,
            AppManagementViewModel => AppManagementTemplate,
            ReviewViewModel => ReviewTemplate,
            LoadingViewModel => LoadingTemplate,
            SummaryViewModel => SummaryTemplate,
            ConfigurationFileViewModel => ConfigurationFileTemplate,
            SetupTargetViewModel => SetupTargetTemplate,
            SelectEnvironmentProviderViewModel => SelectEnvironmentsProviderTemplate,
            EnvironmentCreationOptionsViewModel => EnvironmentCreationOptionsTemplate,
            _ => defaultDataTemplate(),
        };
    }
}
