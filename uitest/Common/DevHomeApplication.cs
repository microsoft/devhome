// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Configurations;
using DevHome.UITest.Pages;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.Extensions;

namespace DevHome.UITest.Common;
public class DevHomeApplication
{
    private readonly DevHomeSession _devHomeSession;

    public static AppConfiguration Configuration { get; } = LoadConfiguration();

    public Guid SessionId => _devHomeSession.Id;

    public WindowsDriver<WindowsElement> Driver => _devHomeSession.Session;

    private WindowsElement DashboardNavigationItem => Driver.FindElementByAccessibilityId("DevHome.Dashboard");

    public DevHomeApplication()
    {
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

    public void TakeScreenshot(string saveFullPath)
    {
        Driver.TakeScreenshot().SaveAsFile(saveFullPath, ScreenshotImageFormat.Png);
    }

    public void Stop()
    {
        _devHomeSession.Stop();
    }

    private static AppConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build()
            .Get<AppConfiguration>();
    }
}
