// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

public class RepoConfigPage : ApplicationPage
{
    private WindowsElement AddRepoButton => Driver.FindElementByName("AddRepositoriesButton");

    public WindowsElement AddRepoHyperlinkButton => Driver.FindElementByAccessibilityId("AddRepoHyperlinkButton");

    public RepoConfigPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }
}
