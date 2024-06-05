// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.Services.WindowsPackageManager.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.UnitTest.Helpers;
using Microsoft.Management.Deployment;
using Moq;

namespace DevHome.SetupFlow.UnitTest.ViewModels;

[TestClass]
public class WinGetPackageJsonDataSourceTest : BaseSetupFlowTest
{
    [TestMethod]
    public void LoadCatalogs_Success_ReturnsWinGetCatalogs()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>
        {
            PackageHelper.CreatePackage("mock1", "winget").Object,
            PackageHelper.CreatePackage("mock2", "winget").Object,
        };
        WindowsPackageManager.Setup(wpm => wpm.GetPackagesAsync(It.IsAny<IList<WinGetPackageUri>>())).ReturnsAsync(expectedPackages);
        WindowsPackageManager.Setup(wpm => wpm.CreatePackageUri(It.IsAny<IWinGetPackage>())).Returns<IWinGetPackage>(p => new WinGetPackageUri(p.CatalogName, p.Id));

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Success.json");

        // Assert
        Assert.AreEqual(1, loadedPackages.Count);
        Assert.AreEqual("mockTitle_1", loadedPackages[0].Name);
        Assert.AreEqual("mockDescription_1", loadedPackages[0].Description);
        Assert.AreEqual(expectedPackages.Count, loadedPackages[0].Packages.Count);
        Assert.AreEqual(expectedPackages[0].Id, loadedPackages[0].Packages.ElementAt(0).Id);
        Assert.AreEqual(expectedPackages[1].Id, loadedPackages[0].Packages.ElementAt(1).Id);
        Assert.AreEqual(expectedPackages[0].CatalogName, loadedPackages[0].Packages.ElementAt(0).CatalogName);
        Assert.AreEqual(expectedPackages[1].CatalogName, loadedPackages[0].Packages.ElementAt(1).CatalogName);
    }

    [TestMethod]
    public void LoadCatalogs_EmptyPackages_ReturnsNoCatalogs()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>();
        WindowsPackageManager.Setup(wpm => wpm.GetPackagesAsync(It.IsAny<IList<WinGetPackageUri>>())).ReturnsAsync(expectedPackages);

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Empty.json");

        // Assert
        Assert.AreEqual(0, loadedPackages.Count);
        WindowsPackageManager.Verify(c => c.GetPackagesAsync(It.IsAny<IList<WinGetPackageUri>>()), Times.Never());
    }

    [TestMethod]
    public void LoadCatalogs_ExceptionThrownWhenGettingPackages_ReturnsNoCatalogs()
    {
        // Configure package manager
        WindowsPackageManager.Setup(wpm => wpm.GetPackagesAsync(It.IsAny<IList<WinGetPackageUri>>())).ThrowsAsync(new FindPackagesException(FindPackagesResultStatus.CatalogError));

        // Act
        var loadedPackages = LoadCatalogsFromJsonDataSource("AppManagementPackages_Success.json");

        // Assert
        Assert.AreEqual(0, loadedPackages.Count);
    }

    [TestMethod]
    public void LoadCatalogs_ExceptionThrownWhenOpeningFile_ThrowsException()
    {
        // Prepare expected package
        var expectedPackages = new List<IWinGetPackage>();
        WindowsPackageManager.Setup(wpm => wpm.GetPackagesAsync(It.IsAny<IList<WinGetPackageUri>>())).ReturnsAsync(expectedPackages);

        // Act/Assert
        var fileName = TestHelpers.GetTestFilePath("file_not_found");
        var jsonDataSource = TestHost.CreateInstance<WinGetPackageJsonDataSource>(fileName);
        Assert.ThrowsException<FileNotFoundException>(() => jsonDataSource.InitializeAsync().GetAwaiter().GetResult());
        Assert.AreEqual(0, jsonDataSource.CatalogCount);
        Assert.AreEqual(0, jsonDataSource.LoadCatalogsAsync().GetAwaiter().GetResult().Count);
    }

    /// <summary>
    /// Load catalogs from a json data source
    /// </summary>
    /// <param name="fileName">Json file name</param>
    /// <returns>List of loaded package catalogs</returns>
    private IList<Models.PackageCatalog> LoadCatalogsFromJsonDataSource(string fileName)
    {
        var fileNamePath = TestHelpers.GetTestFilePath(fileName);
        var jsonDataSource = TestHost.CreateInstance<WinGetPackageJsonDataSource>(fileNamePath);
        jsonDataSource.InitializeAsync().GetAwaiter().GetResult();
        return jsonDataSource.LoadCatalogsAsync().GetAwaiter().GetResult();
    }
}
