// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.UITest.Dialogs;
using DevHome.UITest.Extensions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DevHome.UITest.Pages;

/// <summary>
/// Page model class for the dashboard page
/// </summary>
public class DashboardPage : ApplicationPage
{
    private WindowsElement AddWidgetButton => Driver.FindElementByAccessibilityId("AddWidgetButton");

    private IReadOnlyCollection<WindowsElement> WidgetItems => Driver.FindElementsByAccessibilityId("WidgetItem");

    public IReadOnlyCollection<WidgetControl> DisplayedWidgets => WidgetItems.Select(i => new WidgetControl(Driver, i)).ToList();

    public DashboardPage(WindowsDriver<WindowsElement> driver)
        : base(driver)
    {
    }

    public AddWidgetDialog ClickAddWidgetButton()
    {
        Trace.WriteLine("Clicking add widget button");
        AddWidgetButton.Click();
        return new AddWidgetDialog(Driver, this);
    }

    /// <summary>
    /// Remove all dashboard widgets one by one
    /// </summary>
    public void RemoveAllWidgets()
    {
        // Remove widgets in reverse order. It is slower to remove widgets
        // top-down because the remove animation could cause the automated
        // cursor to miss the 'more options' button, often requiring two
        // attempts to remove a single widget
        Trace.WriteLine("Removing all widgets");
        foreach (var widget in DisplayedWidgets.Reverse())
        {
            widget.Remove();
        }
    }

    public void WaitForWidgetsToBeLoaded()
    {
        Trace.WriteLine("Waiting for the progress ring to disappear");
        Driver.WaitUntilInvisible(ByWindowsAutomation.AccessibilityId("LoadingWidgetsProgressRing"));
    }

    /// <summary>
    /// Control model class for a widget on the dashboard
    /// </summary>
    public class WidgetControl
    {
        private readonly WindowsElement _element;
        private readonly WindowsDriver<WindowsElement> _driver;

        private AppiumWebElement MoreOptionsButton => _element.FindElementByAccessibilityId("WidgetMoreOptionsButton");

        /// <summary>
        /// Gets the remove button on the context menu.
        /// </summary>
        /// <remarks>The remove button on the 'more options' context menu
        /// should be located from the application window</remarks>
        private WindowsElement RemoveButton => _driver.FindElementByAccessibilityId("RemoveWidgetButton");

        public string TitleText => _element.FindElementByAccessibilityId("WidgetTitle").Text;

        public WidgetControl(WindowsDriver<WindowsElement> driver, WindowsElement element)
        {
            _driver = driver;
            _element = element;
        }

        public void Remove()
        {
            // Click on more options then on the remove button.
            // Note: Because widgets move on the dashboard when added/removed,
            // we want to attempt more than one time to remove a widget in case
            // the click was performed during an animation and missed the button.
            Trace.WriteLine($"Removing widget '{TitleText}'");
            _driver
                .RetryUntil(_ =>
                {
                    MoreOptionsButton.Click();
                    return RemoveButton;
                })
                .Click();
        }
    }
}
