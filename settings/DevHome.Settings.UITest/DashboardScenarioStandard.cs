// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.Dashboard.UITest;

[TestClass]
public class DashboardScenarioStandard : DashboardSession
{
    [TestMethod]
    public void DashboardTest1()
    {
        session.FindElementByName("Dashboard").Click();
        WindowsElement title = session.FindElementByName("Dashboard");
        Assert.AreEqual("Dashboard", title.Text);
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Setup(context);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        TearDown();
    }
}
