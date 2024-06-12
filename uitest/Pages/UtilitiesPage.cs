// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using OpenQA.Selenium.Appium.Windows;
using Windows.Win32;

namespace DevHome.UITest.Pages;

public class UtilitiesPage : ApplicationPage
{
    public UtilitiesPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public void LaunchAndVerifyUtility(string utilityName, bool launchAsAdmin = false)
    {
        Trace.WriteLine($"Launching {utilityName} with admin: {launchAsAdmin}");
        WindowsElement utilityView = Driver.FindElementByAccessibilityId(utilityName);

        if (launchAsAdmin)
        {
            var launchAsAdminToggle = utilityView.FindElementByAccessibilityId("AdminToggleAutomationId");
            launchAsAdminToggle.Click();
        }

        var launchButton = utilityView.FindElementByAccessibilityId("LaunchButtonAutomationId");
        launchButton.Click();

        // Wait for the utility to launch
        Thread.Sleep(2000);

        var processes = Process.GetProcessesByName(utilityName);
        Assert.IsTrue(processes.Length == 1, $"{utilityName} as admin:{launchAsAdmin} did not launch");

        if (launchAsAdmin)
        {
            try
            {
                var isAdmin = false;
                SafeFileHandle processToken;
                var result = PInvoke.OpenProcessToken(processes.First().SafeHandle, Windows.Win32.Security.TOKEN_ACCESS_MASK.TOKEN_QUERY, out processToken);
                if (result != 0)
                {
                    var identity = new WindowsIdentity(processToken.DangerousGetHandle());
                    isAdmin = identity?.Owner?.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ?? false;
                }

                Assert.IsTrue(isAdmin, $"{utilityName} did not launch as admin");
            }
            catch (Win32Exception ex)
            {
                Assert.Fail($"Failed to check if {utilityName} launched as admin: {ex.Message}");
            }
        }
    }
}
