// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.UITest.Common;

/// <summary>
/// Base class for all test classes
/// </summary>
public class DevHomeTestBase
{
    protected DevHomeApplication Application => DevHomeApplication.Instance;

    protected string CurrentTestId { get; private set; }

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
        // Set the id for the current test
        CurrentTestId = $"{TestContext.TestName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}";

        // Configure the test tracer
        ConfigureTracer();

        // Start Dev Home
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

    public void RestartDevHome()
    {
        Application.Stop();

        Application.Start();
    }

    /// <summary>
    /// Gets the absolute path for a test asset file
    /// </summary>
    /// <param name="path">Asset file path relative to the TestAssets folder</param>
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
            var fullPath = Path.Combine(screenshotsPath, $"{CurrentTestId}.png");
            Application.TakeScreenshot(fullPath);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"Failed to take a screenshot of the application: {e.Message}");
        }
    }

    /// <summary>
    /// Configure test method tracer
    /// </summary>
    public void ConfigureTracer()
    {
        Trace.AutoFlush = true;
        Trace.Listeners.Clear();

        try
        {
            // Log to file
            var logsPath = Path.Combine(TestRunDirectory, "Logs");
            Directory.CreateDirectory(logsPath);
            var fullPath = Path.Combine(logsPath, $"{CurrentTestId}.txt");
            Trace.Listeners.Add(new TextWriterTraceListener(fullPath));
        }
        catch (Exception e)
        {
            Trace.WriteLine($"Failed to add a trace listener of type {nameof(TextWriterTraceListener)}: {e.Message}");
        }
    }
}
