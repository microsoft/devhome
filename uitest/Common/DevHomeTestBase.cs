// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.UITest.Common;
public class DevHomeTestBase
{
    protected DevHomeApplication Application => DevHomeApplication.Instance;

    /// <summary>
    /// Gets or sets the test context
    /// </summary>
    /// <remarks>Property auto populated at runtime for each test method</remarks>
    public TestContext TestContext { get; set; }

    private string TestDeploymentDir => TestContext.Properties[nameof(TestDeploymentDir)].ToString();

    private string TestRunDirectory => TestContext.Properties[nameof(TestRunDirectory)].ToString();

    [TestInitialize]
    public void TestInitialize()
    {
        Application.Start();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
        {
            TakeScreenshotOfCurrentTest();
        }

        Application.Stop();
    }

    public string GetTestAssetPath(string path)
    {
        return Path.Combine(TestDeploymentDir, "TestAssets", path);
    }

    private void TakeScreenshotOfCurrentTest()
    {
        var screenshotsPath = Path.Combine(TestRunDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotsPath);
        var fullPath = Path.Combine(screenshotsPath, $"{TestContext.TestName}-{Guid.NewGuid()}.png");
        Application.TakeScreenshot(fullPath);
    }
}
