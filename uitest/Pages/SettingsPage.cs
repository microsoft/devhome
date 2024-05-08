// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.UITest.Common;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class SettingsPage : ApplicationPage
{
    public WindowsElement PreferencesButton => Driver.FindElementByAccessibilityId("Preferences");

    public SettingsPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public PreferencesPage NavigateToPreferencesPage()
    {
        Trace.WriteLine("Navigating to Preferences");
        PreferencesButton.Click();
        return new(Driver);
    }
}
