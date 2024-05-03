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

    [DataTestMethod]
    public void PersistOnRebootTest()
    {
        // Arrange
        var dashboard = Application.NavigateToDashboardPage();
        dashboard.RemoveAllWidgets();

        // Act
        dashboard.ClickAddWidgetButton().AddMemoryWidget();
        RestartDevHome();
        dashboard = Application.NavigateToDashboardPage();

        // Assert
        Assert.AreEqual(1, dashboard.DisplayedWidgets.Count);
    }

    [DataTestMethod]
    public void PersistCustomizationOnRebootTest()
    {
        // Arrange
        var dashboard = Application.NavigateToDashboardPage();
        dashboard.RemoveAllWidgets();

        // Act
        var widget = dashboard.ClickAddWidgetButton().AddMemoryWidget();
        widget.MakeSmall();
        RestartDevHome();
        dashboard = Application.NavigateToDashboardPage();

        // Assert
        Assert.AreEqual("Small", dashboard.DisplayedWidgets.First().GetWidgetSize());
    }

    [DataTestMethod]
    public void DragAndDropTest()
    {
        // Arrange
        var dashboard = Application.NavigateToDashboardPage();
        dashboard.RemoveAllWidgets();

        // Act
        var widget = dashboard.ClickAddWidgetButton().AddMemoryWidget();
        dashboard.ClickAddWidgetButton().AddNetworkUsageWidget();
        widget.DragRight();
        RestartDevHome();
        dashboard = Application.NavigateToDashboardPage();

        // Assert
        Assert.AreEqual(2, dashboard.DisplayedWidgets.Count);
        Assert.AreEqual("Network", dashboard.DisplayedWidgets.First().TitleText);
    }
}
