// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Configurations;
using DevHome.UITest.Pages;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.Extensions;

namespace DevHome.UITest.Common;
public sealed class DevHomeApplication
{
    private static readonly Lazy<DevHomeApplication> _instance = new (() => new ());
    private DevHomeSession _devHomeSession;

    public static DevHomeApplication Instance => _instance.Value;

    public AppConfiguration Configuration { get; private set; }

    private WindowsDriver<WindowsElement> Driver => _devHomeSession.Session;

    private WindowsElement DashboardNavigationItem => Driver.FindElementByAccessibilityId("DevHome.Dashboard");

    private DevHomeApplication()
    {
    }

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
        var dashboard = new DashboardPage(Driver);
        dashboard.WaitForWidgetsToBeLoaded();
        return dashboard;
    }

    public void Start()
    {
        _devHomeSession.Start();
        Driver.Manage().Window.Maximize();
    }

    public void Stop()
    {
        _devHomeSession.Stop();
    }

    public void TakeScreenshot(string saveFullPath)
    {
        Driver.TakeScreenshot().SaveAsFile(saveFullPath, ScreenshotImageFormat.Png);
    }
}
