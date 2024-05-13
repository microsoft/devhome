// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class SettingsAccountsPage : ApplicationPage
{
    public WindowsElement AddAccountsButton => Driver.FindElementByAccessibilityId("AddAccountsButton");

    public SettingsAccountsPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }
}
