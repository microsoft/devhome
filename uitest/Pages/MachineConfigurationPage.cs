// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;

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

    public WindowsElement TeachingTip => Driver.FindElementByAccessibilityId("NavigationTeachingTip");

    private WindowsElement TeachingTipCloseButton => Driver.FindElementByAccessibilityId("AlternateCloseButton");

    private WindowsElement CancelButton => Driver.FindElementByName("Cancel");

    private WindowsElement NextButton => Driver.FindElementByAccessibilityId("NavigationNextButton");

    public MachineConfigurationPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public bool DoesNextButtonHaveTeachingTip()
    {
        try
        {
            // If the page declared a NextPageButtonToolTipText getting subtitleTextBlock won't throw.
            var subtitleTextBlock = TeachingTip.FindElementByAccessibilityId("SubtitleTextBlock");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void PointerEnterNextButton()
    {
        var myAction = new Actions(Driver);
        myAction.MoveToElement(NextButton);
        myAction.Build().Perform();
    }

    public void CloseTeachingTip()
    {
        Trace.WriteLine("Closing the teaching tip");
        TeachingTipCloseButton.Click();
    }

    public void CancelSetupFlow()
    {
        Trace.WriteLine("Canceling setup flow");
        CancelButton.Click();
    }

    public RepoConfigPage GoToRepoPage()
    {
        Trace.WriteLine("Going to repo page");
        CloneRepoButton.Click();
        return new RepoConfigPage(Driver);
    }

    public InstallApplicationsPage GoToInstallApplicationsPage()
    {
        Trace.WriteLine("Going to install applications page");
        InstallAppsButton.Click();
        return new InstallApplicationsPage(Driver);
    }

    public RepoConfigPage StartEndToEndSetup()
    {
        Trace.WriteLine("Going to the start of E2E setup.");
        EndToEndSetupButton.Click();
        return new RepoConfigPage(Driver);
    }
}
