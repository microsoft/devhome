// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.UITest.Common;
using DevHome.UITest.Dialogs;
using DevHome.UITest.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.Tests.UITest;

[TestClass]
public class UtilitiesTest : DevHomeTestBase
{
    [TestMethod]
    public void LaunchUtilityTest()
    {
        // Arrange
        var utilities = Application.NavigateToUtilitiesPage();
        utilities.LaunchHostsUtility();
    }
}
