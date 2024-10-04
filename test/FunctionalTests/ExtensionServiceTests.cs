// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using DevHome.Services;
using Moq;

namespace DevHome.Test.FunctionalTests;

[TestClass]
public class ExtensionServiceTests
{
    private readonly Mock<ILocalSettingsService> _localSettingsService = new();

    private readonly Mock<IStringResource> _stringResouce = new();

    [TestInitialize]
    public void TestInitialize()
    {
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
        var extensionService = new ExtensionService(_localSettingsService.Object, _stringResouce.Object);
        var extensionJsonData = await extensionService.GetExtensionJsonDataAsync();
        Assert.IsNotNull(extensionJsonData);

        Assert.IsInstanceOfType(extensionJsonData, typeof(DevHomeExtensionContentData));
    }
}
