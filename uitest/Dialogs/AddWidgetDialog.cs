// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.UITest.Extensions;
using DevHome.UITest.Pages;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Dialogs;

/// <summary>
/// Dialog model class for the add widget dialog
/// </summary>
public class AddWidgetDialog : PageDialog<DashboardPage>
{
    private WindowsElement SSHNavigationItem => Driver.FindElementByAccessibilityId(GetWidgetNavigationItemId("SSH_Wallet"));

    private WindowsElement MemoryNavigationItem => Driver.FindElementByAccessibilityId(GetWidgetNavigationItemId("System_Memory"));

    private WindowsElement NetworkUsageNavigationItem => Driver.FindElementByAccessibilityId(GetWidgetNavigationItemId("System_NetworkUsage"));

    private WindowsElement GPUUsageNavigationItem => Driver.FindElementByAccessibilityId(GetWidgetNavigationItemId("System_GPUUsage"));

    private WindowsElement CPUUsageNavigationItem => Driver.FindElementByAccessibilityId(GetWidgetNavigationItemId("System_CPUUsage"));

    private WindowsElement PinButton => Driver.FindElementByAccessibilityId("PinButton");

    public AddWidgetDialog(WindowsDriver<WindowsElement> driver, DashboardPage dashboardPage)
        : base(driver, dashboardPage)
    {
    }

    public DashboardPage.WidgetControl AddMemoryWidget() => QuickAddWidget(MemoryNavigationItem, "Memory");

    public DashboardPage.WidgetControl AddNetworkUsageWidget() => QuickAddWidget(NetworkUsageNavigationItem, "Network");

    public DashboardPage.WidgetControl AddGPUUsageWidget() => QuickAddWidget(GPUUsageNavigationItem, "GPU");

    public DashboardPage.WidgetControl AddCPUUsageWidget() => QuickAddWidget(CPUUsageNavigationItem, "CPU");

    public DashboardPage.WidgetControl AddSSHWidget() => QuickAddWidget(SSHNavigationItem, "SSH keychain");

    private string GetWidgetNavigationItemId(string widgetId) => $"NavViewItem_{Configuration.Widget.IdPrefix}!App!!{Configuration.Widget.Provider}!!{widgetId}";

    /// <summary>
    /// Add a widget to the dashboard without any configuration
    /// </summary>
    /// <param name="navItemElement">Widget navigation item</param>
    /// <param name="widgetName">Descriptive widget name</param>
    /// <returns>Widget control added to the dashboard</returns>
    private DashboardPage.WidgetControl QuickAddWidget(WindowsElement navItemElement, string widgetName)
    {
        return WaitForWidgetToBeAdded(() =>
        {
            Trace.WriteLine($"Clicking on {widgetName} navigation item");
            navItemElement.Click();

            Trace.WriteLine($"Pinning {widgetName} widget");
            PinButton.Click();
        });
    }

    /// <summary>
    /// Wait for the widget to be added on the dashboard
    /// </summary>
    /// <param name="addWidgetAction">Action for adding the widget</param>
    /// <returns>Widget control added to the dashboard</returns>
    private DashboardPage.WidgetControl WaitForWidgetToBeAdded(Action addWidgetAction)
    {
        var initialWidgetCount = Parent.DisplayedWidgets.Count;
        addWidgetAction();

        Trace.WriteLine($"Waiting for widget to appear before proceeding");
        return Driver
            .RetryUntil(_ =>
            {
                var widgets = Parent.DisplayedWidgets;
                return widgets.ElementAtOrDefault(initialWidgetCount);
            });
    }
}
