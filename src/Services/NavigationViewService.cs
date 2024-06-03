// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Services;

public class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;

    private readonly IPageService _pageService;

    private readonly IScreenReaderService _screenReaderService;

    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public IList<object>? FooterMenuItems => _navigationView?.FooterMenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    public NavigationViewService(INavigationService navigationService, IPageService pageService, IScreenReaderService screenReaderService)
    {
        _navigationService = navigationService;
        _pageService = pageService;
        _screenReaderService = screenReaderService;
    }

    [MemberNotNull(nameof(_navigationView))]
    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;

        _navigationView.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, OnIsPaneOpenChanged);
    }

    private void OnIsPaneOpenChanged(DependencyObject sender, DependencyProperty dp)
    {
        var announcementText = _navigationView!.IsPaneOpen ? "NavigationPaneOpened" : "NavigationPaneClosed";

        _screenReaderService.Announce(announcementText.GetLocalized());
    }

    public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.BackRequested -= OnBackRequested;
            _navigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType) ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => _navigationService.GoBack();

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem != null)
        {
            var invokedItem = (string)args.InvokedItem;
            if (invokedItem != null)
            {
                var currentItem = _navigationService.Frame?.CurrentSourcePageType.Name ?? string.Empty;
                TelemetryFactory.Get<ITelemetry>().Log("NavigationView_Clicked", LogLevel.Critical, new NavigationViewItemEvent(invokedItem, currentItem));
            }
        }

        if (args.IsSettingsInvoked)
        {
            _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        }
        else
        {
            var selectedItem = args.InvokedItemContainer as NavigationViewItem;

            if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
            {
                _navigationService.NavigateTo(pageKey);
            }
        }
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return _pageService.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }
}
