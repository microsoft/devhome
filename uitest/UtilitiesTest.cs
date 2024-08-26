// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.UITest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest;

[TestClass]
public class UtilitiesTest : DevHomeTestBase
{
    [DataTestMethod]
    [DataRow("DevHome.HostsFileEditor", false, DisplayName = "HostsUtility")]
    [DataRow("DevHome.RegistryPreview", false, DisplayName = "RegistryPreview")]
    [DataRow("DevHome.EnvironmentVariables", false, DisplayName = "EnvironmentVariables")]
    [DataRow("DevHome.DevInsights", false, DisplayName = "DevInsights")]
    [DataRow("DevHome.HostsFileEditor", true, DisplayName = "HostsUtility as Admin")]
    [DataRow("DevHome.EnvironmentVariables", true, DisplayName = "EnvironmentVariables as Admin")]
    [DataRow("DevHome.DevInsights", true, DisplayName = "DevInsights as Admin")]
    public void LaunchUtilityTest(string utilityName, bool launchAsAdmin)
    {
        var utilities = Application.NavigateToUtilitiesPage();
        utilities.LaunchAndVerifyUtility(utilityName, launchAsAdmin);
    }

    [TestCleanup]
    public void UtilitiesTestCleanup()
    {
        // Kill all utilities after each test
        string[] utilityNames = { "DevHome.HostsFileEditor", "DevHome.RegistryPreview", "DevHome.EnvironmentVariables", "DevHome.DevInsights" };
        foreach (var utilityName in utilityNames)
        {
            var processes = Process.GetProcessesByName(utilityName);

            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }
}
