// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.UITest.Common;
using DevHome.UITest.Configurations;
using DevHome.UITest.Pages;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Dialogs;

public abstract class PageDialog<T>
    where T : ApplicationPage
{
    protected WindowsDriver<WindowsElement> Driver { get; set; }

    protected AppConfiguration Configuration => DevHomeApplication.Instance.Configuration;

    protected T Parent { get; }

    public PageDialog(WindowsDriver<WindowsElement> driver, T parent)
    {
        Driver = driver;
        Parent = parent;
    }
}
