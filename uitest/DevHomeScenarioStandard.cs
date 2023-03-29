// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest
{
    [TestClass]
    public class DevHomeScenarioStandard : DevHomeSession
    {
        [TestMethod]
        public void DevHomeTest1()
        {
            Assert.AreEqual("Dev Home", session.Title);
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
