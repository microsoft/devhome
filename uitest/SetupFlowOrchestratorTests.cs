// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.UITest;

[TestClass]
public class SetupFlowOrchestratorTests : DevHomeTestBase
{
    [TestMethod]
    public void TestTeachingTipShowsOnFirstPage()
    {
        var machineConfigurationPage = Application.NavigateToMachineConfigurationPage();

        // Test the repo page.  Repo page has a teaching tip.
        machineConfigurationPage.GoToRepoPage();
        Assert.IsTrue(machineConfigurationPage.DoesNextButtonHaveTeachingTip());
        Assert.IsTrue(machineConfigurationPage.TeachingTip.Displayed);

        machineConfigurationPage.CancelSetupFlow();

        // Test the install apps page.  Does not have a teaching tip.
        machineConfigurationPage.GoToInstallApplicationsPage();
        Assert.IsFalse(machineConfigurationPage.DoesNextButtonHaveTeachingTip());

        machineConfigurationPage.CancelSetupFlow();

        // Test the end-to-end button.  First page is repo page.
        machineConfigurationPage.StartEndToEndSetup();
        Assert.IsTrue(machineConfigurationPage.DoesNextButtonHaveTeachingTip());
        Assert.IsTrue(machineConfigurationPage.TeachingTip.Displayed);
    }

    // Ignore test as moving the pointer over the element with Selenium does not trigger the PointerEntered event.
    // The ignore attribute can be removed when we can get Selenium move event to fire the PointerEnetered event.
    [TestMethod]
    [Ignore]
    public void TestTeachingTipHideShowBehavior()
    {
        var machineConfigurationPage = Application.NavigateToMachineConfigurationPage();

        // Test the repo page.  Repo page has a teaching tip.
        machineConfigurationPage.GoToRepoPage();
        Assert.IsTrue(machineConfigurationPage.DoesNextButtonHaveTeachingTip());
        Assert.IsTrue(machineConfigurationPage.TeachingTip.Displayed);

        machineConfigurationPage.CloseTeachingTip();
        Thread.Sleep(1000);
        machineConfigurationPage.PointerEnterNextButton();
        Thread.Sleep(2000);
        Assert.IsTrue(machineConfigurationPage.TeachingTip.Displayed);
    }
}
