// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Drawing;
using System.Formats.Tar;
using DevHome.UITest.Common;
using DevHome.UITest.Dialogs;
using DevHome.UITest.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest;

[TestClass]
public class SettingsTest : DevHomeTestBase
{
    [DataTestMethod]
    public void ThemeTest()
    {
        // Arrange
        var settings = Application.NavigateToSettingsPage();
        var preferences = settings.NavigateToPreferencesPage();

        // Act - Set the theme to dark mode
        preferences.DarkMode();
        var screenshotsPath = Path.Combine(TestRunDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotsPath);
        var fullPath = Path.Combine(screenshotsPath, $"{CurrentTestId}-DarkMode.png");
        Application.TakeScreenshot(fullPath);
        var bitmap = new Bitmap(fullPath);
        var pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);

        // Assert - Dark mode
        Assert.IsTrue(pixel.R < 100);
        Assert.IsTrue(pixel.G < 100);
        Assert.IsTrue(pixel.B < 100);

        // Act - Set the theme to light mode
        preferences.LightMode();
        Directory.CreateDirectory(screenshotsPath);
        fullPath = Path.Combine(screenshotsPath, $"{CurrentTestId}-LightMode.png");
        Application.TakeScreenshot(fullPath);
        bitmap = new Bitmap(fullPath);
        pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);

        // Assert - Light mode
        Assert.IsTrue(pixel.R > 200);
        Assert.IsTrue(pixel.G > 200);
        Assert.IsTrue(pixel.B > 200);

        // Resets the theme to default mode
        preferences.DefaultMode();
    }
}
