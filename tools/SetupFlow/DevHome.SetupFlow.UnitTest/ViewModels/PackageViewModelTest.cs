// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.UnitTest.Helpers;

namespace DevHome.SetupFlow.UnitTest.ViewModels;

[TestClass]
public class PackageViewModelTest : BaseSetupFlowTest
{
    [TestMethod]
    public void CreatePackageViewModel_Success()
    {
        var packageCatalog = PackageHelper.CreatePackageCatalog(10);
        var packageCatalogViewModel = TestHost!.CreateInstance<PackageCatalogViewModel>(packageCatalog);
        var expectedPackages = packageCatalog.Packages.ToList();
        var packages = packageCatalogViewModel.Packages.ToList();
        Assert.AreEqual(expectedPackages.Count, packages.Count);
        for (var i = 0; i < expectedPackages.Count; ++i)
        {
            Assert.AreEqual(expectedPackages[i].Name, packages[i].Name);
            Assert.AreEqual(expectedPackages[i].ImageUri, packages[i].ImageUri);
            Assert.AreEqual(expectedPackages[i].Version, packages[i].Version);
        }
    }
}
