// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Common;

/// <summary>
/// Class for connecting to the Windows Application Driver server and
/// initializing a Driver client for interacting with a new instance of Dev Home
/// </summary>
public sealed class DevHomeSession
{
    private readonly string _driverUrl;
    private readonly string _appId;

    /// <summary>
    /// Gets a driver for interacting with Dev Home
    /// </summary>
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
        Driver?.Quit();
        Driver = null;
    }
}
