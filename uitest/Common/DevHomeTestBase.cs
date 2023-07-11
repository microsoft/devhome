// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.UITest.Common;

/// <summary>
/// Base class for all test classes
/// </summary>
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

    /// <summary>
    /// Gets the absolute path for a test asset file
    /// </summary>
    /// <param name="path">Asset file path</param>
    /// <returns>Absolute path for the provided test asset</returns>
    public string GetTestAssetPath(string path)
    {
        return Path.Combine(TestDeploymentDir, "TestAssets", path);
    }

    /// <summary>
    /// Take a screenshot of the Dev Home application used for the currently
    /// executing task
    /// </summary>
    private void TakeScreenshotOfCurrentTest()
    {
        try
        {
            var screenshotsPath = Path.Combine(TestRunDirectory, "Screenshots");
            Directory.CreateDirectory(screenshotsPath);

            // Add a GUID suffix to the file name to ensure that test methods
            // executed multiple times with different parameters don't
            // overwrite each other
            var fullPath = Path.Combine(screenshotsPath, $"{TestContext.TestName}-{Guid.NewGuid()}.png");
            Application.TakeScreenshot(fullPath);
        }
        catch
        {
            // Failed to take a screenshot
        }
    }
}
