// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.Contracts.Services;

public interface INavigationViewService
{
    IList<object>? MenuItems
    {
        get;
    }

    IList<object>? FooterMenuItems
    {
        get;
    }

    object? SettingsItem
    {
        get;
    }

    void Initialize(NavigationView navigationView);

    void UnregisterEvents();

    NavigationViewItem? GetSelectedItem(Type pageType);
}
