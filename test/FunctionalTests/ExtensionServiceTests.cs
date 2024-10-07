// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using DevHome.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace DevHome.Test.FunctionalTests;

[TestClass]
public class ExtensionServiceTests
{
    private readonly Mock<ILocalSettingsService> _localSettingsService = new();

    private readonly Mock<IStringResource> _stringResouce = new();

    private IHost? _hostService;

    [TestInitialize]
    public void TestInitialize()
    {
        _hostService = CreateHost();

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
        var extensionService = _hostService!.GetService<IExtensionService>();
        var extensionJsonData = await extensionService.GetExtensionJsonDataAsync();
        Assert.IsNotNull(extensionJsonData);

        Assert.IsInstanceOfType(extensionJsonData, typeof(DevHomeExtensionContentData));
    }

    private IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_localSettingsService!.Object);
                services.AddSingleton(_stringResouce!.Object);
                services.AddSingleton<IExtensionService, ExtensionService>();
                services.AddHttpClient();
            }).Build();
    }
}
