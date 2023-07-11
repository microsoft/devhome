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

    public WindowsDriver<WindowsElement> Driver { get; private set; }

    public DevHomeSession(string driverUrl, string appId)
    {
        _driverUrl = driverUrl;
        _appId = appId;
    }

    /// <summary>
    /// Create and start a new session to bring up an instance of the Dev Home
    /// application
    /// </summary>
    public void Start()
    {
        if (Driver == null)
        {
            var options = new AppiumOptions();
            options.AddAdditionalCapability("deviceName", "WindowsPC");
            options.AddAdditionalCapability("platformName", "Windows");
            options.AddAdditionalCapability("app", _appId);

            Driver = new WindowsDriver<WindowsElement>(new Uri(_driverUrl), options);
            Assert.IsNotNull(Driver);

            // Set implicit timeout to 5 seconds to make element search to retry every 500 ms for at most ten times
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }
    }

    /// <summary>
    /// Close the application and delete the session
    /// </summary>
    public void Stop()
    {
        if (Driver != null)
        {
            Driver.Quit();
            Driver = null;
        }
    }
}
