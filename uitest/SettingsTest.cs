// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Drawing;
using DevHome.UITest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest;

[TestClass]
public class SettingsTest : DevHomeTestBase
{
    private float GetProportionOfLightPixels(Bitmap bitmap)
    {
        var numOfPixels = 30;
        var numOfLightPixels = 0;
        var rand = new Random();

        for (var i = 0; i < numOfPixels; i++)
        {
            var w = rand.Next(bitmap.Width);
            var h = rand.Next(bitmap.Height);
            var pixel = bitmap.GetPixel(w, h);
            if (pixel.GetBrightness() > 0.5f)
            {
                ++numOfLightPixels;
            }
        }

        return numOfLightPixels / (numOfPixels * 1f);
    }

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
        Thread.Sleep(2000);
        Application.TakeScreenshot(fullPath);
        var bitmap = new Bitmap(fullPath);

        // Assert - Dark mode
        Assert.IsTrue(GetProportionOfLightPixels(bitmap) < 0.5f);

        // Act - Set the theme to light mode
        preferences.LightMode();
        Directory.CreateDirectory(screenshotsPath);
        fullPath = Path.Combine(screenshotsPath, $"{CurrentTestId}-LightMode.png");
        Thread.Sleep(2000);
        Application.TakeScreenshot(fullPath);
        bitmap = new Bitmap(fullPath);

        // Assert - Light mode
        Assert.IsTrue(GetProportionOfLightPixels(bitmap) > 0.5f);

        // Resets the theme to default mode
        preferences.DefaultMode();
    }
}
