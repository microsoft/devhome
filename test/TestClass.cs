// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace DevHome.Test;

/*
 * TODO: Write unit tests.
 * https://docs.microsoft.com/visualstudio/test/getting-started-with-unit-testing
 * https://docs.microsoft.com/visualstudio/test/using-microsoft-visualstudio-testtools-unittesting-members-in-unit-tests
 * https://docs.microsoft.com/visualstudio/test/run-unit-tests-with-test-explorer
 * https://github.com/microsoft/devhome/issues/604
 */

[TestClass]
public class TestClass
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Debug.WriteLine("ClassInitialize");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        Debug.WriteLine("ClassCleanup");
    }

    [TestInitialize]
    public void TestInitialize()
    {
        Debug.WriteLine("TestInitialize");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Debug.WriteLine("TestCleanup");
    }

    [TestMethod]
    public void TestMethod()
    {
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TestHelpersIngestion()
    {
        Microsoft.Internal.Windows.DevHome.Helpers.Helpers helpers = new();
        Assert.AreEqual("This is a test", helpers.Test());
    }

    [TestMethod]
    public void TestExperimentHelpers_UnrecognizedKey_ReturnFalse()
    {
        Microsoft.Internal.Windows.DevHome.Helpers.Experimentation.Experiment experiment = new();
        Assert.IsFalse(experiment.IsEnabled("unknown_key"));
    }
}
