// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;

namespace DevHome.UITest.Extensions;

public static class WindowsDriverExtensions
{
    private const int DefaultTimeoutInMS = 60_000;
    private const int DefaultPollingIntervalInMS = 500;

    /// <summary>
    /// Create an instance of <see cref="DefaultWait{T}"/>
    /// </summary>
    /// <param name="driver">Driver instance</param>
    /// <param name="timeout">Wait timeout</param>
    /// <param name="pollingInterval">Action interval</param>
    /// <returns>Instance of <see cref="DefaultWait{T}"/></returns>
    public static DefaultWait<WindowsDriver<WindowsElement>> Wait(
        this WindowsDriver<WindowsElement> driver,
        TimeSpan timeout,
        TimeSpan pollingInterval)
    {
        return new(driver)
        {
            Timeout = timeout,
            PollingInterval = pollingInterval,
        };
    }

    /// <summary>
    /// Create an instance of <see cref="DefaultWait{T}"/>
    /// </summary>
    /// <param name="driver">Driver instance</param>
    /// <param name="timeoutInMS">Wait timeout in milliseconds</param>
    /// <param name="pollingIntervalInMS">Action interval in milliseconds</param>
    /// <returns>Instance of <see cref="DefaultWait{T}"/></returns>
    public static DefaultWait<WindowsDriver<WindowsElement>> Wait(
        this WindowsDriver<WindowsElement> driver,
        int timeoutInMS = DefaultTimeoutInMS,
        int pollingIntervalInMS = DefaultPollingIntervalInMS)
    {
        return driver.Wait(TimeSpan.FromMilliseconds(timeoutInMS), TimeSpan.FromMilliseconds(pollingIntervalInMS));
    }

    /// <summary>
    /// Create an instance of <see cref="DefaultWait{T}"/> which ignores all <see cref="WebDriverException"/> exceptions
    /// </summary>
    /// <param name="driver">Driver instance</param>
    /// <param name="timeoutInMS">Wait timeout in milliseconds</param>
    /// <param name="pollingIntervalInMS">Action interval in milliseconds</param>
    /// <returns>Instance of <see cref="DefaultWait{T}"/></returns>
    public static DefaultWait<WindowsDriver<WindowsElement>> WaitAndIgnoreExceptions(
        this WindowsDriver<WindowsElement> driver,
        int timeoutInMS = DefaultTimeoutInMS,
        int pollingIntervalInMS = DefaultPollingIntervalInMS)
    {
        var wait = driver.Wait(timeoutInMS, pollingIntervalInMS);
        wait.IgnoreExceptionTypes(typeof(WebDriverException));
        return wait;
    }

    /// <summary>
    /// Wait until an element is invisible or timeout
    /// </summary>
    /// <param name="driver">Driver instance</param>
    /// <param name="by">Element locator</param>
    /// <param name="timeoutInMS">Wait timeout in milliseconds</param>
    /// <param name="pollingIntervalInMS">Action interval in milliseconds</param>
    public static void WaitUntilInvisible(
        this WindowsDriver<WindowsElement> driver,
        By by,
        int timeoutInMS = DefaultTimeoutInMS,
        int pollingIntervalInMS = DefaultPollingIntervalInMS)
    {
        driver
            .Wait(timeoutInMS, pollingIntervalInMS)
            .Until(_ =>
            {
                try
                {
                    return !driver.FindElement(by).Displayed;
                }
                catch (WebDriverException)
                {
                    return true;
                }
            });
    }

    /// <summary>
    /// Wait until an element is visible or timeout
    /// </summary>
    /// <param name="driver">Driver instance</param>
    /// <param name="by">Element locator</param>
    /// <param name="timeoutInMS">Wait timeout in milliseconds</param>
    /// <param name="pollingIntervalInMS">Action interval in milliseconds</param>
    public static WindowsElement WaitUntilVisible(
        this WindowsDriver<WindowsElement> driver,
        By by,
        int timeoutInMS = DefaultTimeoutInMS,
        int pollingIntervalInMS = DefaultPollingIntervalInMS)
    {
        return driver
            .Wait(timeoutInMS, pollingIntervalInMS)
            .Until(_ =>
            {
                try
                {
                    var element = driver.FindElement(by);
                    return element.Displayed ? element : null;
                }
                catch (WebDriverException)
                {
                    return null;
                }
            });
    }

    /// <summary>
    /// Retry an action until returning true, an object not null or timeout
    /// </summary>
    /// <param name="driver">Driver instance</param>
    /// <param name="retryAction">Action to retry</param>
    /// <param name="timeoutInMS">Wait timeout in milliseconds</param>
    /// <param name="pollingIntervalInMS">Action interval in milliseconds</param>
    public static T RetryUntil<T>(
        this WindowsDriver<WindowsElement> driver,
        Func<WindowsDriver<WindowsElement>, T> retryAction,
        int timeoutInMS = DefaultTimeoutInMS,
        int pollingIntervalInMS = DefaultPollingIntervalInMS)
    {
        return driver
            .WaitAndIgnoreExceptions(timeoutInMS, pollingIntervalInMS)
            .Until(retryAction);
    }
}
