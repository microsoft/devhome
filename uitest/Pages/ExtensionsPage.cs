// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class ExtensionsPage : ApplicationPage
{
    public WindowsElement GetUpdatesButton => Driver.FindElementByAccessibilityId("GetUpdatesButton");

    public WindowsElement GitHubInstallButton => Driver.FindElementByAccessibilityId("Install_Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe");

    public WindowsElement GitHubMoreOptionsButton => Driver.FindElementByAccessibilityId("MoreOptions_Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe");

    public WindowsElement SampleExtensionCard => Driver.FindElementByAccessibilityId("Dev Home Sample Extension (Dev)");

    public WindowsElement SampleExtensionMoreOptionsButton => Driver.FindElementByAccessibilityId("MoreOptions_Microsoft.Windows.DevHomeSampleExtension_8wekyb3d8bbwe");

    public ExtensionsPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public SampleExtensionSettingsPage NavigateToSampleExtensionSettingsPage()
    {
        Trace.WriteLine("Navigating to SampleExtensionSettings");
        SampleExtensionCard.Click();

        return new(Driver);
    }
}
