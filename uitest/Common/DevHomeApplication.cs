// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Configurations;
using DevHome.UITest.Pages;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.Extensions;

namespace DevHome.UITest.Common;

/// <summary>
/// Singleton class representing a Dev Home application instance
/// </summary>
public sealed class DevHomeApplication
{
    private static readonly Lazy<DevHomeApplication> _instance = new (() => new ());
    private DevHomeSession _devHomeSession;

    /// <summary>
    /// Gets the singleton Dev Home application instance
    /// </summary>
    public static DevHomeApplication Instance => _instance.Value;

    /// <summary>
    /// Gets the application settings configuration
    /// </summary>
    /// <remarks>Content populated by the appsettings JSON files</remarks>
    public AppConfiguration Configuration { get; private set; }

    private WindowsElement DashboardNavigationItem => _devHomeSession.Driver.FindElementByAccessibilityId("DevHome.Dashboard");

    private WindowsElement MachineConfigurationNavigationItem => _devHomeSession.Driver.FindElementByAccessibilityId("DevHome.SetupFlow");

    private DevHomeApplication()
    {
    }

    /// <summary>
    /// Initialize the singleton instance
    /// </summary>
    /// <param name="appSettingsMode">Application settings mode (local, canary, etc ...)</param>
    public void Initialize(string appSettingsMode)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{appSettingsMode}.json", optional: true)
            .Build()
            .Get<AppConfiguration>();

        _devHomeSession = new (Configuration.WindowsApplicationDriverUrl, $"{Configuration.PackageFamilyName}!App");
    }

    public DashboardPage NavigateToDashboardPage()
    {
        DashboardNavigationItem.Click();
        var dashboard = new DashboardPage(_devHomeSession.Driver);
        dashboard.WaitForWidgetsToBeLoaded();
        return dashboard;
    }

    public MachineConfigurationPage NavigateToMachineConfigurationPage()
    {
        MachineConfigurationNavigationItem.Click();
        return new (_devHomeSession.Driver);
    }

    /// <summary>
    /// Start Dev Home application
    /// </summary>
    public void Start()
    {
        _devHomeSession.Start();
        _devHomeSession.Driver.Manage().Window.Maximize();
    }

    /// <summary>
    /// Stop Dev Home application
    /// </summary>
    public void Stop()
    {
        _devHomeSession.Stop();
    }

    /// <summary>
    /// Take a screenshot of the Dev Home application
    /// </summary>
    /// <param name="saveFullPath">Storage location of the screenshot</param>
    public void TakeScreenshot(string saveFullPath)
    {
        _devHomeSession.Driver.TakeScreenshot().SaveAsFile(saveFullPath, ScreenshotImageFormat.Png);
    }
}
