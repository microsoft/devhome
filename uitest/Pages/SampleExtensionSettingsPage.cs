// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class SampleExtensionSettingsPage : ApplicationPage
{
    // public WindowsElement GetUpdatesButton => Driver.FindElementByAccessibilityId("GetUpdatesButton");

    // public WindowsElement GitHubInstallButton => Driver.FindElementByAccessibilityId("Install_Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe");

    // public WindowsElement GitHubMoreOptionsButton => Driver.FindElementByAccessibilityId("MoreOptions_Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bb// we");
    public WindowsElement SampleExtensionBreadcrumbBar => Driver.FindElementByAccessibilityId("BreadcrumbBar");

    /*
     * To add:
     * - Navigate to SampleExtensionSettings (Application to Extensions on nav bar, then Dev Home Sample Extension button
     * - click Dev Home Sample Extension button, then click on Dev Home Sample Extension dropdown
     * - assert that the navigation worked (check the Breadcrumb Bar is Extensions > Dev Home Sample Extension (Dev)
     *
     */
    public SampleExtensionSettingsPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    /*
     * Adaptive Card tests:
     * - If sample extension settings page is using an adaptive card, test that it is visible
     * - Page changes
     * - Theme changes
     * - accessible name for adaptive card (and subcomponents)
     *
     * WebView tests:
     * - If sample extension settings page is using a WebView, test that it is visible
     * - Test that webview matches theme changes
     * - Test webview size/shape adapts to window size changes (matches page)
     * - WebView with null and empty URL -> error message and adaptive card
     * - Test case with null webview2 and no valid adaptive card
     * - test accessible name for webview
     */
}
