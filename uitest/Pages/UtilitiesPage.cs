// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.UITest.Dialogs;
using DevHome.UITest.Extensions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;

namespace DevHome.UITest.Pages;

public class UtilitiesPage : ApplicationPage
{
    // public WindowsElement HostsUtilityView => Driver.FindElementByAccessibilityId("HostsFileEditorAutomationId");
    public UtilitiesPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public void LaunchHostsUtility()
    {
        Trace.WriteLine("Launching Hosts Utility");
        WindowsElement hostsUtilityView = Driver.FindElementByAccessibilityId("HostsFileEditorAutomationId");
        var button = hostsUtilityView.FindElementByAccessibilityId("LaunchButtonAutomationId");
        button.Click();
    }

    public void LaunchHostsUtilityAsAdmin()
    {
        // Trace.WriteLine("Launching Hosts Utility");
        // HostsUtilityView.FindElementByAccessibilityId("AdminToggleAutomationId").Click();
        // HostsUtilityView.FindElementByAccessibilityId("LaunchButtonAutomationId").Click();
    }
}
