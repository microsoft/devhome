// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Common;
using DevHome.UITest.Configurations;
using DevHome.UITest.Pages;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Dialogs;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1649:File name should match first type name",
    Justification = "Template class")]
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
