// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

/// <summary>
/// Page model class for the machine configuration page
/// </summary>
public class IntroducingDevHomePage : ApplicationPage
{
    private WindowsElement GetStartedButton => Driver.FindElementByAccessibilityId("WhatsNewPage_GetStartedButton");

    private WindowsElement ExploreExtensionsButton => Driver.FindElementByAccessibilityId("WhatsNewPage_ExtensionsButton");

    private WindowsElement PinWidgetsButton => Driver.FindElementByAccessibilityId("WhatsNewPage_DevDashButton");

    private WindowsElement ConnectAccountsButton => Driver.FindElementByAccessibilityId("WhatsNewPage_DevIdButton");

    public IntroducingDevHomePage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public MachineConfigurationPage ClickGetStarted()
    {
        Trace.WriteLine("Clicking Get Started");
        GetStartedButton.Click();
        return new MachineConfigurationPage(Driver);
    }

    public ExtensionsPage ClickExploreExtensions()
    {
        Trace.WriteLine("Clicking Explore Extensions");
        ExploreExtensionsButton.Click();
        return new ExtensionsPage(Driver);
    }

    public DashboardPage ClickPinWidgets()
    {
        Trace.WriteLine("Clicking Pin Widgets");
        PinWidgetsButton.Click();
        return new DashboardPage(Driver);
    }

    public SettingsAccountsPage ClickConnectAccounts()
    {
        Trace.WriteLine("Clicking Connect Accounts");
        ConnectAccountsButton.Click();
        return new SettingsAccountsPage(Driver);
    }
}
