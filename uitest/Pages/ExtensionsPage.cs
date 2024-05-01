// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

/// <summary>
/// Page model class for the machine configuration page
/// </summary>
public class ExtensionsPage : ApplicationPage
{
    public WindowsElement GetUpdatesButton => Driver.FindElementByAccessibilityId("GetUpdatesButton");

    public ExtensionsPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }
}
