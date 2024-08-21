// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.DevInsights.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.DevInsights.Controls;

internal sealed class ExternalToolsManagementButton : Button
{
    private const string UnregisterButtonGlyph = "\uECC9";

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly string _pinMenuItemText = CommonHelper.GetLocalizedString("PinMenuItemText");
    private readonly string _unpinMenuItemText = CommonHelper.GetLocalizedString("UnpinMenuItemRawText");
    private readonly string _unregisterMenuItemText = CommonHelper.GetLocalizedString("UnregisterMenuItemRawText");
    private readonly ExternalToolsHelper _externalTools;

    public event EventHandler<ExternalTool>? ExternalToolLaunchRequest;

    public ExternalToolsManagementButton()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _externalTools = Application.Current.GetService<ExternalToolsHelper>();

        InitializeExternalTools();
    }

    private void InitializeExternalTools()
    {
        if (_externalTools.AllExternalTools.Count > 0)
        {
            var flyout = new MenuFlyout();
            this.ContextFlyout = flyout;

            foreach (var tool in _externalTools.AllExternalTools)
            {
                AddExternalToolToContextMenu(flyout, tool);
                tool.PropertyChanged += ExternalToolItem_PropertyChanged;
            }
        }

        // We have to cast to INotifyCollectionChanged explicitly because the CollectionChanged
        // event in ReadOnlyObservableCollection is protected.
        ((INotifyCollectionChanged)_externalTools.AllExternalTools).CollectionChanged += ExternalTools_CollectionChanged;
    }

    private void ExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (ExternalTool tool in e.NewItems)
                {
                    // If this button doens't have a flyout, create one
                    if (this.ContextFlyout is not MenuFlyout flyout)
                    {
                        flyout = new MenuFlyout();
                        this.ContextFlyout = flyout;
                    }

                    AddExternalToolToContextMenu(flyout, tool);

                    // Listen for tool changes
                    tool.PropertyChanged += ExternalToolItem_PropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
            {
                foreach (ExternalTool tool in e.OldItems)
                {
                    tool.PropertyChanged -= ExternalToolItem_PropertyChanged;

                    // If this button doens't have a flyout, create one
                    if (this.ContextFlyout is MenuFlyout flyout)
                    {
                        RemoveExternalToolFromContextMenu(flyout, tool);

                        // If we've removed all the items from the flyout, remove the flyout
                        if (flyout.Items.Count == 0)
                        {
                            this.ContextFlyout = null;
                        }
                    }
                }
            }
        });
    }

    private void AddExternalToolToContextMenu(MenuFlyout menuFlyout, ExternalTool tool)
    {
        menuFlyout.Items.Add(CreateContextMenuItemForTool(tool));
    }

    private void RemoveExternalToolFromContextMenu(MenuFlyout menuFlyout, ExternalTool tool)
    {
        foreach (var menuItem in menuFlyout.Items.Where(x => x.Tag == tool))
        {
            tool.PropertyChanged -= ExternalToolItem_PropertyChanged;
            menuFlyout.Items.Remove(menuItem);
            break;
        }
    }

    // This creates a Menu Flyout item for an external tool. It also creates a sub-menu item for pinning/unpinning
    // and unregistering the tool.
    private MenuFlyoutItem CreateContextMenuItemForTool(ExternalTool tool)
    {
        var imageIcon = tool.GetIcon();

        var menuItem = new MenuFlyoutItem
        {
            Text = tool.Name,
            Tag = tool,
            Icon = imageIcon,
        };
        menuItem.Click += ExternalToolMenuItem_Click;

        var pinMenuSubItemItem = new MenuFlyoutItem
        {
            Text = _pinMenuItemText,
            Icon = GetFontIcon(CommonHelper.PinGlyph),
            Tag = tool,
        };
        pinMenuSubItemItem.Click += ExternalToolPinUnpin_Click;

        var unPinMenuSubItemItem = new MenuFlyoutItem
        {
            Text = _unpinMenuItemText,
            Icon = GetFontIcon(CommonHelper.UnpinGlyph),
            Tag = tool,
        };
        unPinMenuSubItemItem.Click += ExternalToolPinUnpin_Click;

        var unRegisterMenuSubItemItem = new MenuFlyoutItem
        {
            Text = _unregisterMenuItemText,
            Icon = GetFontIcon(UnregisterButtonGlyph),
            Tag = tool,
        };
        unRegisterMenuSubItemItem.Click += UnregisterMenuItem_Click;

        var menuSubItemFlyout = new MenuFlyout();

        menuSubItemFlyout.Items.Add(pinMenuSubItemItem);
        menuSubItemFlyout.Items.Add(unPinMenuSubItemItem);
        menuSubItemFlyout.Items.Add(unRegisterMenuSubItemItem);

        if (tool.IsPinned)
        {
            pinMenuSubItemItem.Visibility = Visibility.Collapsed;
        }
        else
        {
            unPinMenuSubItemItem.Visibility = Visibility.Collapsed;
        }

        menuItem.ContextFlyout = menuSubItemFlyout;

        return menuItem;
    }

    private void ExternalToolItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ExternalTool tool)
        {
            Debug.Assert(false, "Why are we getting notified of a tool property change without a tool?");
            return;
        }

        _dispatcher.TryEnqueue(() =>
        {
            MenuFlyout? flyout = this.ContextFlyout as MenuFlyout;
            Debug.Assert(flyout is not null, "Why does this button not have a menu flyout?. It should have been populated in CreateContextMenuItemForTool");

            // Update the submenu item for this tool
            foreach (MenuFlyoutItem menuItem in flyout.Items.Where(x => x.Tag == tool))
            {
                // Update the name if it's changed
                menuItem.Text = tool.Name;

                var imageIcon = tool.GetIcon();

                menuItem.Icon = imageIcon;

                var menuSubItemFlyout = menuItem.ContextFlyout as MenuFlyout;
                Debug.Assert(menuSubItemFlyout != null, "It's expected this menuItem has a sub flyout. See CreateContextMenuItemForTool");

                var pinSubItemItem = menuSubItemFlyout.Items[0] as MenuFlyoutItem;
                Debug.Assert(pinSubItemItem != null, "The subflyout should have 2 items. The first is to pin the tool. See CreateContextMenuItemForTool");

                var unPinSubItemItem = menuSubItemFlyout.Items[1] as MenuFlyoutItem;
                Debug.Assert(unPinSubItemItem != null, "The second flyout should be to unpin the tool. See CreateContextMenuItemForTool");

                // Toggle the visibily of the pin and unpin menu items based on the pinned state of the tool
                pinSubItemItem.Visibility = tool.IsPinned ? Visibility.Collapsed : Visibility.Visible;
                unPinSubItemItem.Visibility = tool.IsPinned ? Visibility.Visible : Visibility.Collapsed;
                break;
            }
        });
    }

    public void ExternalToolMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem clickedMenuItem && clickedMenuItem.Tag is ExternalTool tool)
        {
            ExternalToolLaunchRequest?.Invoke(this, tool);
        }
    }

    private void ExternalToolPinUnpin_Click(object sender, RoutedEventArgs e)
    {
        ExternalTool tool = GetToolFromSender(sender);
        tool.IsPinned = !tool.IsPinned;
        HideFlyout(sender);
    }

    public void UnregisterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExternalTool tool = GetToolFromSender(sender);
        tool.UnregisterTool();

        HideFlyout(sender);
    }

    private ExternalTool GetToolFromSender(object sender)
    {
        MenuFlyoutItem? clickedMenuItem = sender as MenuFlyoutItem;
        Debug.Assert(clickedMenuItem != null, "Why is this null? This should be a MenuFlyoutItem");

        ExternalTool? tool = clickedMenuItem.Tag as ExternalTool;
        Debug.Assert(tool != null, "The menuflyout items should have external tools as their tag. See CreateContextMenuItemForTool");

        return tool;
    }

    private void HideFlyout(object sender)
    {
        this.ContextFlyout.Hide();
    }

    private FontIcon GetFontIcon(string s)
    {
        var fontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"];

        var icon = new FontIcon
        {
            FontFamily = fontFamily,
            Glyph = s,
        };

        return icon;
    }
}
