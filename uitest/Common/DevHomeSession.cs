// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Common;

public sealed class DevHomeSession
{
    private readonly string _driverUrl;
    private readonly string _appId;

    public Guid Id { get; } = Guid.NewGuid();

    public WindowsDriver<WindowsElement> Session { get; private set; }

    public DevHomeSession(string driverUrl, string appId)
    {
        _driverUrl = driverUrl;
        _appId = appId;
    }

    public void Start()
    {
        if (Session == null)
        {
            // Create a new session to bring up an instance of the Dev Home application
            // Note: Multiple calculator windows (instances) share the same process Id
            var options = new AppiumOptions();
            options.AddAdditionalCapability("deviceName", "WindowsPC");
            options.AddAdditionalCapability("platformName", "Windows");
            options.AddAdditionalCapability("app", _appId);

            Session = new WindowsDriver<WindowsElement>(new Uri(_driverUrl), options);
            Assert.IsNotNull(Session);

            // Set implicit timeout to 5 seconds to make element search to retry every 500 ms for at most ten times
            Session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }
    }

    public void Stop()
    {
        // Close the application and delete the session
        if (Session != null)
        {
            Session.Quit();
            Session = null;
        }
    }
}
