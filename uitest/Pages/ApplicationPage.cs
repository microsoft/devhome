// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Common;
using DevHome.UITest.Configurations;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;
public class ApplicationPage
{
    protected WindowsDriver<WindowsElement> Driver { get; set; }

    protected AppConfiguration Configuration => DevHomeApplication.Configuration;

    public ApplicationPage(WindowsDriver<WindowsElement> driver)
    {
        Driver = driver;
    }
}
