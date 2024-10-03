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

    [TestInitialize]
    public void TestInitialize()
    {
        _localSettingsService.Setup(localSettingsService =>
            localSettingsService.GetPathToPackageLocation())
            .Returns(Directory.GetCurrentDirectory());
    }

    [TestMethod]
    public async Task ExtensionServiceReturnsValidExtensionJsonDataObject()
    {
        var extensionService = new ExtensionService(_localSettingsService.Object);
        var extensionJsonData = await extensionService.GetExtensionJsonDataAsync();
        Assert.IsNotNull(extensionJsonData);

        Assert.IsInstanceOfType(extensionJsonData, typeof(DevHomeExtensionJsonData));
    }
}
