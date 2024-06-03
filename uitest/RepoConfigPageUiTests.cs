// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        Assert.IsTrue(repoConfigPage.AddRepoHyperlinkButton.Displayed);
    }
}
