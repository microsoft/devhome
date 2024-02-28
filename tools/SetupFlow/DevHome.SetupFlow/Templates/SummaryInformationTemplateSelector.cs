// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Templates;

public class SummaryInformationTemplateSelector : DataTemplateSelector
{
    public DataTemplate CloneRepoDataTemplate
    {
        get; set;
    }

    public DataTemplate ConfigurationDataTemplate
    {
        get; set;
    }

    public DataTemplate CreateDevDriveDataTemplate
    {
        get; set;
    }

    public DataTemplate InstallPackageDataTemplate
    {
        get; set;
    }

    public DataTemplate EmptyDataTemplate
    {
        get; set;
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            CloneRepoSummaryInformationViewModel => CloneRepoDataTemplate,
            ConfigurationSummaryInformationViewModel => ConfigurationDataTemplate,
            CreateDevDriveSummaryInformationViewModel => CreateDevDriveDataTemplate,
            InstallPackageSummaryInformationViewModel => InstallPackageDataTemplate,
            _ => EmptyDataTemplate,
        };
    }
}
