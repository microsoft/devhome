// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.Tests.UITest;

public class SampleToolSession
{
    private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
    private const string DevHomeAppId = "Microsoft.DevHome_8wekyb3d8bbwe!App";

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable CA2211 // Non-constant fields should not be visible
    protected static WindowsDriver<WindowsElement> session;
#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore SA1401 // Fields should be private

    public static void Setup(TestContext context)
    {
        if (session == null)
        {
            // Create a new session to bring up an instance of the Dev Home application
            // Note: Multiple calculator windows (instances) share the same process Id
            AppiumOptions options = new AppiumOptions();
            options.AddAdditionalCapability("deviceName", "WindowsPC");
            options.AddAdditionalCapability("platformName", "Windows");
            options.AddAdditionalCapability("app", DevHomeAppId);

            session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), options);
            Assert.IsNotNull(session);

            // Set implicit timeout to 1.5 seconds to make element search to retry every 500 ms for at most three times
            session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1.5);
        }
    }

    public static void TearDown()
    {
        // Close the application and delete the session
        if (session != null)
        {
            session.Quit();
            session = null;
        }
    }
}
