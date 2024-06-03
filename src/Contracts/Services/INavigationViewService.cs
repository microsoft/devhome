// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
