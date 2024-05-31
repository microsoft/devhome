// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
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
    private static readonly Lazy<DevHomeApplication> _instance = new(() => new());
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

    private WindowsElement IntroducingDevHomeNavigationItem => _devHomeSession.Driver.FindElementByAccessibilityId("WhatsNew");

    private WindowsElement SettingsNavigationItem => _devHomeSession.Driver.FindElementByAccessibilityId("SettingsItem");

    private WindowsElement ExtensionsNavigationItem => _devHomeSession.Driver.FindElementByAccessibilityId("Extensions");

    private WindowsElement UtilitiesNavigationItem => _devHomeSession.Driver.FindElementByAccessibilityId("DevHome.Utilities");

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

        _devHomeSession = new(Configuration.WindowsApplicationDriverUrl, $"{Configuration.PackageFamilyName}!App");
    }

    public string GetMainPageId()
    {
        return _devHomeSession.Driver.FindElementByAccessibilityId("MainPage").Id;
    }

    public DashboardPage NavigateToDashboardPage()
    {
        Trace.WriteLine("Navigating to Dashboard");
        DashboardNavigationItem.Click();
        var dashboard = new DashboardPage(_devHomeSession.Driver);
        dashboard.WaitForWidgetsToBeLoaded();
        return dashboard;
    }

    public MachineConfigurationPage NavigateToMachineConfigurationPage()
    {
        Trace.WriteLine("Navigating to Machine Configuration");
        MachineConfigurationNavigationItem.Click();
        return new(_devHomeSession.Driver);
    }

    public IntroducingDevHomePage NavigateToIntroducingDevHomePage()
    {
        Trace.WriteLine("Navigating to Introducing Dev Home");
        IntroducingDevHomeNavigationItem.Click();
        return new(_devHomeSession.Driver);
    }

    public SettingsPage NavigateToSettingsPage()
    {
        Trace.WriteLine("Navigating to Settings");
        SettingsNavigationItem.Click();
        return new(_devHomeSession.Driver);
    }

    public ExtensionsPage NavigateToExtensionsPage()
    {
        Trace.WriteLine("Navigating to Extensions");
        ExtensionsNavigationItem.Click();
        return new(_devHomeSession.Driver);
    }

    public UtilitiesPage NavigateToUtilitiesPage()
    {
        Trace.WriteLine("Navigating to Utilities");
        UtilitiesNavigationItem.Click();
        var utilities = new UtilitiesPage(_devHomeSession.Driver);
        return utilities;
    }

    /// <summary>
    /// Start Dev Home application
    /// </summary>
    public void Start()
    {
        Trace.WriteLine("Starting Dev Home");
        _devHomeSession.Start();
        _devHomeSession.Driver.Manage().Window.Maximize();
    }

    /// <summary>
    /// Stop Dev Home application
    /// </summary>
    public void Stop()
    {
        Trace.WriteLine("Stopping Dev Home");
        _devHomeSession.Stop();
    }

    /// <summary>
    /// Take a screenshot of the Dev Home application
    /// </summary>
    /// <param name="targetPath">Storage location of the PNG screenshot</param>
    public void TakeScreenshot(string targetPath)
    {
        Trace.WriteLine($"Taking a screenshot and saving file at '{targetPath}'");
        _devHomeSession.Driver.TakeScreenshot().SaveAsFile(targetPath, ScreenshotImageFormat.Png);
    }
}
