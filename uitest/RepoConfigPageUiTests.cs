// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.UITest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.UITest;

[TestClass]
public class RepoConfigPageUiTests : DevHomeTestBase
{
    [TestMethod]
    public void TestNewRepoConfigPage()
    {
        var machineConfigurationPage = Application.NavigateToMachineConfigurationPage();
        var repoConfigPage = machineConfigurationPage.GoToRepoPage();

        // This button will be displayed when no repos are selected.
        Assert.IsTrue(repoConfigPage.AddRepoHyperLinkButton.Displayed);
    }
}
