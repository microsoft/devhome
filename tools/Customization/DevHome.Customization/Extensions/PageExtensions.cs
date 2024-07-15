// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.Customization.Views;

namespace DevHome.Customization.Extensions;

public static class PageExtensions
{
    public static void ConfigureCustomizationPages(this IPageService pageService)
    {
        pageService.Configure<FileExplorerViewModel, FileExplorerPage>();
        pageService.Configure<DevDriveInsightsViewModel, DevDriveInsightsPage>();
        pageService.Configure<VirtualizationFeatureManagementViewModel, VirtualizationFeatureManagementPage>();
        pageService.Configure<GeneralSystemViewModel, GeneralSystemPage>();
    }
}
