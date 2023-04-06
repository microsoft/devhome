// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.SetupFlow.UITest;

[TestClass]
public class SetupFlowScenarioStandard : SetupFlowSession
{
    [TestMethod]
    public void SetupFlowTest1()
    {
        session.FindElementByName("Machine Configuration").Click();
        WindowsElement title = session.FindElementByName("Add packages");
        Assert.AreEqual("Add packages", title.Text);
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
