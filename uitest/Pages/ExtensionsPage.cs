// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class ExtensionsPage : ApplicationPage
{
    public WindowsElement GetUpdatesButton => Driver.FindElementByAccessibilityId("GetUpdatesButton");

    public WindowsElement GitHubInstallButton => Driver.FindElementByAccessibilityId("Install_Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe");

    public WindowsElement GitHubMoreOptionsButton => Driver.FindElementByAccessibilityId("MoreOptions_Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe");

    public ExtensionsPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }
}
