// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using static System.Collections.Specialized.BitVector32;

namespace DevHome.SetupFlow.UITest;

[TestClass]
public class SetupFlowScenarioStandard : SetupFlowSession
{
    [TestMethod]
    public void SetupFlowTest1()
    {
        session.FindElementByName("Dev Setup tool").Click();
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
