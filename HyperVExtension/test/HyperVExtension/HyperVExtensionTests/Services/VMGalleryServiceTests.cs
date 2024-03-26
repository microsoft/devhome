// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.Common.Extensions;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using HyperVExtension.Services;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

[TestClass]
public class VMGalleryServiceTests : HyperVExtensionTestsBase
{
    // SHA256 hash of the gallery symbol byte array
    private readonly string _gallerySymbolByteArrayHash = "843AC23B1736B4487EC81CF7C07DDD9BB46AE5B7818C2C3843D99D62FA75F3C9";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = JsonSourceGenerationContext.Default,
        AllowTrailingCommas = true,
    };

    [TestMethod]
    public void VMGalleryService_Can_Retrieve_VMGalleryJson()
    {
        // Arrange
        var vmGalleryService = TestHost!.GetService<IVMGalleryService>();
        var expectedJsonObj = JsonSerializer.Deserialize(TestGallery, typeof(VMGalleryImageList), _jsonOptions) as VMGalleryImageList;
        SetupGalleryHttpContent();

        // Act
        var vmGalleryImageList = vmGalleryService.GetGalleryImagesAsync().Result;

        // Assert
        Assert.IsNotNull(vmGalleryImageList);
        var symbolHash = vmGalleryImageList.Images[0].Symbol.Hash.Split(":").Last();

        Assert.IsTrue(string.Equals(_gallerySymbolByteArrayHash, symbolHash, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(expectedJsonObj?.Images.Count, vmGalleryImageList.Images.Count);
        Assert.AreEqual(expectedJsonObj?.Images[0].Name, vmGalleryImageList.Images[0].Name);
    }

    [TestMethod]
    public void VMGalleryService_ReturnsCorrectDiskFileNameWithExtension()
    {
        // Arrange
        var vmGalleryService = TestHost!.GetService<IVMGalleryService>();
        SetupGalleryHttpContent();

        // Act
        var vmGalleryImageList = vmGalleryService.GetGalleryImagesAsync().Result;
        var actualFileName = vmGalleryService.GetDownloadedArchiveFileName(vmGalleryImageList.Images[0]);
        var expectedFileName = $"{GalleryDiskHash}.zip";

        Assert.IsTrue(string.Equals(expectedFileName, actualFileName, StringComparison.OrdinalIgnoreCase));
    }
}
