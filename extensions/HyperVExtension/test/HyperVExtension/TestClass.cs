// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace HyperVExtension.UnitTest;

/*
 * Class showing how to write unit tests for the extension.
 * https://docs.microsoft.com/visualstudio/test/getting-started-with-unit-testing
 * https://docs.microsoft.com/visualstudio/test/using-microsoft-visualstudio-testtools-unittesting-members-in-unit-tests
 * https://docs.microsoft.com/visualstudio/test/run-unit-tests-with-test-explorer
 */

[TestClass]
public class TestClass : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

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
}
