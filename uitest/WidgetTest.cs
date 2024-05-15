// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.UITest.Common;
using DevHome.UITest.Dialogs;
using DevHome.UITest.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest;

[TestClass]
public class WidgetTest : DevHomeTestBase
{
    [TestMethod]
    public void RemoveWidgetTest()
    {
        // Arrange
        var dashboard = Application.NavigateToDashboardPage();
        dashboard.RemoveAllWidgets();

        // Act
        var initialWidgetCount = dashboard.DisplayedWidgets.Count;
        dashboard.ClickAddWidgetButton().AddNetworkUsageWidget();
        var oneWidgetCount = dashboard.DisplayedWidgets.Count;
        dashboard.RemoveAllWidgets();
        var finalWidgetCount = dashboard.DisplayedWidgets.Count;

        // Assert
        Assert.AreEqual(0, initialWidgetCount);
        Assert.AreEqual(1, oneWidgetCount);
        Assert.AreEqual(0, finalWidgetCount);
    }

    [DataTestMethod]
    [DataRow(new string[] { "SSH keychain" }, DisplayName = "SSH")]
    [DataRow(new string[] { "GPU" }, DisplayName = "GPU")]
    [DataRow(new string[] { "CPU" }, DisplayName = "CPU")]
    [DataRow(new string[] { "Network" }, DisplayName = "Network")]
    [DataRow(new string[] { "Memory" }, DisplayName = "Memory")]
    [DataRow(new string[] { "CPU", "GPU", "Network", "Memory", "SSH keychain" }, DisplayName = "CPU, GPU, Network, Memory, SSH")]
    public void AddWidgetsTest(string[] widgetTitles)
    {
        // Arrange
        Dictionary<string, Func<AddWidgetDialog, DashboardPage.WidgetControl>> widgetMap = new()
        {
            ["GPU"] = dialog => dialog.AddGPUUsageWidget(),
            ["CPU"] = dialog => dialog.AddCPUUsageWidget(),
            ["Network"] = dialog => dialog.AddNetworkUsageWidget(),
            ["Memory"] = dialog => dialog.AddMemoryWidget(),
            ["SSH keychain"] = dialog => dialog.AddSSHWidget(),
        };
        var dashboard = Application.NavigateToDashboardPage();
        dashboard.RemoveAllWidgets();

        // Act
        var widgets = widgetTitles.Select(w => widgetMap[w](dashboard.ClickAddWidgetButton())).ToList();

        // Assert
        Assert.AreEqual(widgetTitles.Length, dashboard.DisplayedWidgets.Count);
        CollectionAssert.AreEqual(widgetTitles, widgets.Select(w => w.TitleText).ToList());
    }
}
