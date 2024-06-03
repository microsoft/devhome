// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.UITest.Common;
using DevHome.UITest.Configurations;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

/// <summary>
/// Application page base class for all Dev Home pages
/// </summary>
public class ApplicationPage
{
    protected WindowsDriver<WindowsElement> Driver { get; set; }

    protected AppConfiguration Configuration => DevHomeApplication.Instance.Configuration;

    public ApplicationPage(WindowsDriver<WindowsElement> driver)
    {
        Driver = driver;
    }
}
