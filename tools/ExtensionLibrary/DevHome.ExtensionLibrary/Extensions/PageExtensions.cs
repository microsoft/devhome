// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.ExtensionLibrary.ViewModels;
using DevHome.ExtensionLibrary.Views;

namespace DevHome.ExtensionLibrary.Extensions;

public static class PageExtensions
{
    public static void ConfigureExtensionLibraryPages(this IPageService pageService)
    {
        pageService.Configure<ExtensionLibraryViewModel, ExtensionLibraryView>();
        pageService.Configure<ExtensionSettingsViewModel, ExtensionSettingsPage>();
        pageService.Configure<ExtensionNavigationViewModel, ExtensionNavigationPage>();
    }
}
