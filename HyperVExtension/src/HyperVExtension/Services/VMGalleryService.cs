// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using HyperVExtension.Providers;
using Serilog;
using Windows.Storage;

namespace HyperVExtension.Services;

/// <summary>
/// Service that performs operations specific to the VM gallery.
/// </summary>
public sealed class VMGalleryService : IVMGalleryService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", ComponentName);

    private const string ComponentName = "VMGalleryService";

    private readonly IDownloaderService _downloaderService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = JsonSourceGenerationContext.Default,
        AllowTrailingCommas = true,
    };

    public VMGalleryService(IDownloaderService downloaderService)
    {
        _downloaderService = downloaderService;
    }

    private static readonly Uri VmGalleryUrl = new("https://go.microsoft.com/fwlink/?linkid=851584");

    private VMGalleryImageList _imageList = new();

    /// <inheritdoc cref="IVMGalleryService.GetGalleryImagesAsync"/>
    public async Task<VMGalleryImageList> GetGalleryImagesAsync()
    {
        // If we have already downloaded the image list before, return it.
        if (_imageList.Images.Count > 0)
        {
            return _imageList;
        }

        var emptyList = new VMGalleryImageList();
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();

            // This should be quick as the file is around 28.0 KB, so a 5 minute timeout should be ok on the worst network.
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

            // get the JSON data
            var resultJson = await _downloaderService.DownloadStringAsync(VmGalleryUrl.AbsoluteUri, cancellationTokenSource.Token);
            _imageList = JsonSerializer.Deserialize(resultJson, typeof(VMGalleryImageList), _jsonOptions) as VMGalleryImageList ?? emptyList;

            // Now we need to download the base64 images for the symbols (icons). So they can be used within an adaptive card.
            foreach (var image in _imageList.Images)
            {
                if (!string.IsNullOrEmpty(image.Symbol.Uri))
                {
                    var byteArray = await _downloaderService.DownloadByteArrayAsync(image.Symbol.Uri, cancellationTokenSource.Token);
                    if (!ValidateFileSha256Hash(byteArray, image.Symbol.Hash))
                    {
                        _log.Error($"Symbol Hash '{image.Symbol.Hash}' validation failed for image with name '{image}'. Symbol uri: '{image.Symbol}'");
                        continue;
                    }

                    image.Symbol.Base64Image = Convert.ToBase64String(byteArray);
                }

                if (!string.IsNullOrEmpty(image.Disk.Uri))
                {
                    var totalSizeOfDisk = await _downloaderService.GetHeaderContentLength(new Uri(image.Disk.Uri), cancellationTokenSource.Token);
                    if (ulong.TryParse(image.Requirements.DiskSpace, CultureInfo.InvariantCulture, out var requiredDiskSpace))
                    {
                        image.Disk.SizeInBytes = (ulong)totalSizeOfDisk;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Unable to retrieve VM gallery images");
        }

        return _imageList;
    }

    /// <inheritdoc cref="IVMGalleryService.GetDownloadedArchiveFileName"/>
    public string GetDownloadedArchiveFileName(VMGalleryImage image)
    {
        var hash = image.Disk.Hash;
        var hexValues = hash.Split(':').Last();

        // Now get the file extension from the web uri. The web uri is the location of the zipped image on the web.
        // we can expect this last segment to be the file name plus the extension.
        var webUri = new Uri(image.Disk.Uri);
        var fileExtension = webUri.Segments.Last().Split('.').Last();
        return $"{hexValues}.{fileExtension}";
    }

    /// <summary>
    /// Validates that the file has the correct SHA256 hash and has not been tampered with.
    /// </summary>
    /// <param name="file">File on the file system that we will compare the hash to</param>
    /// <returns>True if the hash validation was successful and false otherwise</returns>
    public async Task<bool> ValidateFileSha256Hash(StorageFile file)
    {
        var fileStream = await file.OpenStreamForReadAsync();
        var hashedFileStream = SHA256.HashData(fileStream);
        var hashedFileString = BitConverter.ToString(hashedFileStream).Replace("-", string.Empty);

        // For our usage the file name before the extension is the original hash of the file.
        // We'll compare that to the current hash of the file.
        return string.Equals(hashedFileString, file.Name.Split('.').First(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that the file has the correct SHA256 hash and has not been tampered with.
    /// </summary>
    /// <param name="byteArray">ByteArray received from the web that we will compare the hash to</param>
    /// <returns>True if the hash validation was successful and false otherwise</returns>
    public bool ValidateFileSha256Hash(byte[] byteArray, string hashOfGalleryItem)
    {
        var hashedByteArray = SHA256.HashData(byteArray);
        var hashedString = BitConverter.ToString(hashedByteArray).Replace("-", string.Empty);

        // remove the sha256: prefix from the hash
        var hashOfGalleryItemWithoutPrefix = hashOfGalleryItem.Split(':').Last();

        // For our usage the file name before the extension is the original hash of the file.
        // We'll compare that to the current hash of the file.
        return string.Equals(hashedString, hashOfGalleryItemWithoutPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
