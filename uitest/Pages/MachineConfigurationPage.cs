// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

/// <summary>
/// Page model class for the machine configuration page
/// </summary>
public class MachineConfigurationPage : ApplicationPage
{
    private WindowsElement EndToEndSetupButton => Driver.FindElementByAccessibilityId("EndToEndSetupButton");

    private WindowsElement DSCConfigurationButton => Driver.FindElementByAccessibilityId("DSCConfigurationButton");

    private WindowsElement CloneRepoButton => Driver.FindElementByAccessibilityId("CloneRepoButton");

    private WindowsElement InstallAppsButton => Driver.FindElementByAccessibilityId("InstallAppsButton");

    private WindowsElement DevDriveButton => Driver.FindElementByAccessibilityId("DevDriveButton");

    public MachineConfigurationPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public RepoConfigPage GoToRepoPage()
    {
        Trace.WriteLine("Going to repo page");
        CloneRepoButton.Click();
        return new RepoConfigPage(Driver);
    }
}
