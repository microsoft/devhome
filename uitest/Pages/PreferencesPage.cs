// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.UITest.Common;
using DevHome.UITest.Extensions;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class PreferencesPage : ApplicationPage
{
    public WindowsElement ThemeButton => Driver.FindElementByAccessibilityId("ThemeSelectionComboBox");

    public WindowsElement DefaultThemeButton => Driver.FindElementByAccessibilityId("SettingsThemeDefault");

    public WindowsElement LightThemeButton => Driver.FindElementByAccessibilityId("SettingsThemeLight");

    public WindowsElement DarkThemeButton => Driver.FindElementByAccessibilityId("SettingsThemeDark");

    public PreferencesPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public void DefaultMode()
    {
        Trace.WriteLine("Setting Default Mode");
        ThemeButton.Click();
        DefaultThemeButton.Click();
        Driver.Wait(1);
    }

    public void LightMode()
    {
        Trace.WriteLine("Setting Light Mode");
        ThemeButton.Click();
        LightThemeButton.Click();
        Driver.Wait(1);
    }

    public void DarkMode()
    {
        Trace.WriteLine("Setting Dark Mode");
        ThemeButton.Click();
        DarkThemeButton.Click();
        Driver.Wait(1);
    }
}
