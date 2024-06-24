// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.UITest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest;

[TestClass]
public class IntroducingTest : DevHomeTestBase
{
    [TestMethod]
    public void GetStartedTest()
    {
        // Arrange
        var introPage = Application.NavigateToIntroducingDevHomePage();

        // Act
        var machineConfigurationPage = introPage.ClickGetStarted();

        // Assert
        Assert.IsTrue(machineConfigurationPage.EndToEndSetupButton.Displayed);
    }

    [TestMethod]
    public void ExploreExtensionsTest()
    {
        // Arrange
        var introPage = Application.NavigateToIntroducingDevHomePage();

        // Act
        var extensionsPage = introPage.ClickExploreExtensions();

        // Assert
        Assert.IsTrue(extensionsPage.GetUpdatesButton.Displayed);
    }

    [TestMethod]
    public void PinWidgetsTest()
    {
        // Arrange
        var introPage = Application.NavigateToIntroducingDevHomePage();

        // Act
        var dashboardPage = introPage.ClickPinWidgets();

        // Assert
        Assert.IsTrue(dashboardPage.AddWidgetButton.Displayed);
    }

    [TestMethod]
    public void ConnectAccountsTest()
    {
        // Arrange
        var introPage = Application.NavigateToIntroducingDevHomePage();

        // Act
        var accountsPage = introPage.ClickConnectAccounts();

        // Assert
        Assert.IsTrue(accountsPage.AddAccountsButton.Displayed);
    }
}
