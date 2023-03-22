﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.UnitTest.Helpers;

namespace DevHome.SetupFlow.UnitTest;

[TestClass]
public class PackageCatalogViewModelTest : BaseSetupFlowTest
{
    [TestMethod]
    public void NewPackageCatalogViewModel_Success()
    {
        var packageCatalog = PackageHelper.CreatePackageCatalog(10);
        var packageViewModel = TestHost!.CreateInstance<PackageCatalogViewModel>(packageCatalog);
        Assert.AreEqual(packageCatalog.Name, packageViewModel.Name);
        Assert.AreEqual(packageCatalog.Description, packageViewModel.Description);
        Assert.AreEqual(packageCatalog.Packages.Count, packageViewModel.Packages.Count);
    }
}
