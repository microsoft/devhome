// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using static System.Collections.Specialized.BitVector32;

namespace DevHome.Tests.UITest
{
    [TestClass]
    public class SampleToolScenarioStandard : SampleToolSession
    {
        // [TestMethod] // Test is just a sample and should not run
        public void SampleToolTest1()
        {
            WindowsElement title = session.FindElementByName("Sample Tool");
            Assert.AreEqual("Sample Tool", title.Text);
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
}
