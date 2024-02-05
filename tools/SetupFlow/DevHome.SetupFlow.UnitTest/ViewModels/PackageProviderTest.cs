// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.UnitTest.Helpers;

namespace DevHome.SetupFlow.UnitTest.ViewModels;

[TestClass]
public class PackageProviderTest : BaseSetupFlowTest
{
    [TestMethod]
    public void CreateOrGet_NewPackage_ReturnsNewPackage()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result = packageProvider.CreateOrGet(mockPackage); // Not cached

        // Assert
        Assert.AreEqual(mockPackage.UniqueKey, result.UniqueKey);
        Assert.AreEqual(0, packageProvider.SelectedPackages.Count);
    }

    [TestMethod]
    public void CreateOrGet_SamePackageNoCache_ReturnsDifferentPackages()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result1 = packageProvider.CreateOrGet(mockPackage); // Not cached
        var result2 = packageProvider.CreateOrGet(mockPackage); // Not cached

        // Assert
        Assert.AreEqual(mockPackage.UniqueKey, result1.UniqueKey);
        Assert.AreEqual(mockPackage.UniqueKey, result2.UniqueKey);
        Assert.AreNotEqual(result1, result2);
        Assert.AreEqual(0, packageProvider.SelectedPackages.Count);
    }

    [TestMethod]
    public void CreateOrGet_SamePackageCache_ReturnsSamePackage()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result1 = packageProvider.CreateOrGet(mockPackage, cachePermanently: true); // Cached permanently
        var result2 = packageProvider.CreateOrGet(mockPackage);                         // Same object from cache

        // Assert
        Assert.AreEqual(mockPackage.UniqueKey, result1.UniqueKey);
        Assert.AreEqual(mockPackage.UniqueKey, result2.UniqueKey);
        Assert.AreEqual(result1, result2);
        Assert.AreEqual(0, packageProvider.SelectedPackages.Count);
    }

    [TestMethod]
    public void CreateOrGet_SelectPackage_AddsPackageToSelectedPackagesCollection()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result = packageProvider.CreateOrGet(mockPackage);  // Not cached
        result.IsSelected = true;                               // Cached temporarily

        // Assert
        Assert.AreEqual(1, packageProvider.SelectedPackages.Count);
    }

    [TestMethod]
    public void CreateOrGet_SelectPackageThenUnselect_PackageIsCachedTemporarily()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result1 = packageProvider.CreateOrGet(mockPackage); // Not cached
        result1.IsSelected = true;                              // Cached temporarily
        var result2 = packageProvider.CreateOrGet(mockPackage); // Same object from cache
        result1.IsSelected = false;                             // Removed from cache
        var result3 = packageProvider.CreateOrGet(mockPackage); // New object, not cached

        // Assert
        Assert.AreEqual(result1, result2);
        Assert.AreNotEqual(result1, result3);
        Assert.AreEqual(0, packageProvider.SelectedPackages.Count);
    }

    [TestMethod]
    public void CreateOrGet_SelectPackageThenCacheAndUnselect_PackageNotRemovedFromCache()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result1 = packageProvider.CreateOrGet(mockPackage);                         // Not cached
        result1.IsSelected = true;                                                      // Cached temporarily
        var result2 = packageProvider.CreateOrGet(mockPackage, cachePermanently: true); // Cached permanently
        result1.IsSelected = false;                                                     // Not removed from cache
        var result3 = packageProvider.CreateOrGet(mockPackage);                         // Same object from cache

        // Assert
        Assert.AreEqual(result1, result2);
        Assert.AreEqual(result1, result3);
        Assert.AreEqual(0, packageProvider.SelectedPackages.Count);
    }

    [TestMethod]
    public void CreateOrGet_SelectCachedPackageThenUnselect_PackageNotRemovedFromCache()
    {
        // Arrange
        var mockPackage = PackageHelper.CreatePackage("mock").Object;
        var packageProvider = TestHost.GetService<PackageProvider>();

        // Act
        var result1 = packageProvider.CreateOrGet(mockPackage, cachePermanently: true); // Cached permanently
        result1.IsSelected = true;                                                      // No-op
        var result2 = packageProvider.CreateOrGet(mockPackage);                         // Same object from cache
        result1.IsSelected = false;                                                     // Not removed from cache
        var result3 = packageProvider.CreateOrGet(mockPackage);                         // Same object from cache

        // Assert
        Assert.AreEqual(result1, result2);
        Assert.AreEqual(result1, result3);
        Assert.AreEqual(0, packageProvider.SelectedPackages.Count);
    }
}
