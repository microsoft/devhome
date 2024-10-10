// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using DevHome.Services;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;
using Moq;

namespace DevHome.Test.FunctionalTests;

[TestClass]
public class ExtensionServiceTests
{
    private readonly Mock<ILocalSettingsService> _localSettingsService = new();

    private readonly Mock<IStringResource> _stringResouce = new();

    private readonly Mock<IWinGet> _winGet = new();

    private readonly WinGetPackageUri _packageUri = new("x-ms-winget://msstore/9MV8F79FGXTR");

    private readonly Mock<IWinGetPackage> _winGetPackage = new();

    private async Task<IList<IWinGetPackage>> MockGetPackagesAsync()
    {
        await Task.CompletedTask;
        return new List<IWinGetPackage>() { _winGetPackage.Object };
    }

    [TestInitialize]
    public void TestInitialize()
    {
        // Setup WinGet to return at least one valid package.
        _winGet
            .Setup(manager => manager.CreateMsStoreCatalogPackageUri(It.IsAny<string>()))
            .Returns(_packageUri);

        _winGet
            .Setup(manager => manager.GetPackagesAsync(It.IsAny<List<WinGetPackageUri>>()))
            .Returns(MockGetPackagesAsync);

        _winGetPackage.Setup(package => package.Id).Returns("9MV8F79FGXTR");

        _stringResouce
            .Setup(strResource => strResource.GetLocalized(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);

        _localSettingsService.Setup(localSettingsService =>
            localSettingsService.GetPathToPackageLocation())
            .Returns(Directory.GetCurrentDirectory());
    }

    [TestMethod]
    public async Task ExtensionServiceReturnsValidExtensionJsonDataObject()
    {
        var extensionService = new ExtensionService(_localSettingsService.Object, _stringResouce.Object, _winGet.Object);
        var extensionJsonData = await extensionService.GetExtensionJsonDataAsync();
        Assert.IsNotNull(extensionJsonData);

        Assert.IsInstanceOfType(extensionJsonData, typeof(DevHomeExtensionContentData));
    }
}
