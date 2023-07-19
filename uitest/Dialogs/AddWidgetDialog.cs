// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Extensions;
using DevHome.UITest.Pages;
using OpenQA.Selenium.Appium;
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

    public DashboardPage.WidgetControl AddMemoryWidget() => QuickAddWidget(MemoryNavigationItem);

    public DashboardPage.WidgetControl AddNetworkUsageWidget() => QuickAddWidget(NetworkUsageNavigationItem);

    public DashboardPage.WidgetControl AddGPUUsageWidget() => QuickAddWidget(GPUUsageNavigationItem);

    public DashboardPage.WidgetControl AddCPUUsageWidget() => QuickAddWidget(CPUUsageNavigationItem);

    public DashboardPage.WidgetControl AddSSHWidget(string configFilePath)
    {
        return WaitForWidgetToBeAdded(() =>
        {
            SSHNavigationItem.Click();

            // Wait for the widget to be rendered before configuring and
            // pinning it
            // TODO: Can we use AccessibilityId for adaptive cards forms?
            var container = Driver.WaitUntilVisible(ByWindowsAutomation.ClassName("NamedContainerAutomationPeer"));
            var input = container.FindElementByClassName("TextBox");
            input.Clear();
            input.SendKeys(configFilePath);
            var submit = container.FindElementByClassName("Button");
            submit.Click();
            PinButton.Click();
        });
    }

    private string GetWidgetNavigationItemId(string widgetId) => $"NavViewItem_{Configuration.Widget.IdPrefix}!App!!{Configuration.Widget.Provider}!!{widgetId}";

    /// <summary>
    /// Add a widget to the dashboard without any configuration
    /// </summary>
    /// <param name="element">Widget navigation item</param>
    /// <returns>Widget control added to the dashboard</returns>
    private DashboardPage.WidgetControl QuickAddWidget(WindowsElement element)
    {
        return WaitForWidgetToBeAdded(() =>
        {
            element.Click();
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
        return Driver
            .RetryUntil(_ =>
            {
                var widgets = Parent.DisplayedWidgets;
                return widgets.ElementAtOrDefault(initialWidgetCount);
            });
    }
}
