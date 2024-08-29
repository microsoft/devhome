// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using Serilog;
using Windows.Storage;

namespace HyperVExtension.Services;

/// <summary>
/// Service that performs operations specific to the VM gallery.
/// </summary>
public sealed class VMGalleryService : IVMGalleryService, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", ComponentName);

    private const string ComponentName = "VMGalleryService";

    private readonly IDownloaderService _downloaderService;

    private readonly SemaphoreSlim _gallerySemaphore = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = JsonSourceGenerationContext.Default,
        AllowTrailingCommas = true,
    };

    private bool _disposed;

    public const ulong Windows11ImageExtractedSize = 48318382080UL; // 45 GB in bytes

    public const ulong Windows10ImageExtractedSize = 16106127360UL; // 15 GB in bytes

    public const ulong DefaultImageExtractedSize = 10737418240UL; // 10 GB in bytes

    public VMGalleryService(IDownloaderService downloaderService)
    {
        _downloaderService = downloaderService;
    }

    private static readonly Uri _vmGalleryUrl = new("https://go.microsoft.com/fwlink/?linkid=851584");

    private readonly VMGalleryImageList _imageList = new();

    /// <inheritdoc cref="IVMGalleryService.GetGalleryImagesAsync"/>
    public async Task<VMGalleryImageList> GetGalleryImagesAsync()
    {
        try
        {
            await _gallerySemaphore.WaitAsync();

            // If we have already downloaded the image list before, return it.
            if (_imageList.Images.Count > 0)
            {
                return _imageList;
            }

            var emptyList = new VMGalleryImageList();
            var cancellationTokenSource = new CancellationTokenSource();

            // This should be quick as the file is around 28.0 KB, so a 5 minute timeout should be ok on the worst network.
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

            // get the JSON data
            var resultJson = await _downloaderService.DownloadStringAsync(_vmGalleryUrl.AbsoluteUri, cancellationTokenSource.Token);
            var imageList = JsonSerializer.Deserialize(resultJson, typeof(VMGalleryImageList), _jsonOptions) as VMGalleryImageList ?? emptyList;

            // Now we need to download the base64 images for the symbols (icons). So they can be used within an adaptive card.
            await Parallel.ForEachAsync(imageList.Images, async (image, cancellationToken) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(image.Symbol.Uri))
                    {
                        var byteArray = await _downloaderService.DownloadByteArrayAsync(image.Symbol.Uri, cancellationTokenSource.Token);
                        if (!ValidateFileSha256Hash(byteArray, image.Symbol.Hash))
                        {
                            _log.Error($"Symbol Hash '{image.Symbol.Hash}' validation failed for image with name '{image}'. Symbol uri: '{image.Symbol}'");
                        }
                        else
                        {
                            image.Symbol.Base64Image = Convert.ToBase64String(byteArray);
                        }
                    }

                    if (string.IsNullOrEmpty(image.Disk.Uri))
                    {
                        throw new InvalidDataException($"Disk Uri for gallery image {image} empty");
                    }

                    var totalSizeOfDisk = await _downloaderService.GetHeaderContentLength(new Uri(image.Disk.Uri), cancellationTokenSource.Token);
                    image.Disk.ArchiveSizeInBytes = (ulong)totalSizeOfDisk;
                    var requiredSpaceFromJson = ulong.Parse(image.Requirements.DiskSpace, CultureInfo.InvariantCulture);
                    image.Disk.ExtractedFileRequiredFreeSpace = GetRequiredExtractedFileSize(image);
                    _imageList.Images.Add(image);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Unable to retrieve VM gallery data for {image}");
                }
            });

            _imageList.Images = _imageList.Images.OrderBy(image => image.Name).ToList();
            return _imageList;
        }
        finally
        {
            _gallerySemaphore.Release();
        }
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

    /// <summary>
    /// Gets the expected size of the vhdx file within the archive file that is downloaded from the VM
    /// gallery. Note: This is a temporary measure until the "diskSpace" property of the requirements key in
    /// the VM gallery is updated with the correct diskspace values. The values in the VM gallery do not accurately
    /// reflect the size of the extracted file. #See Dev Home issue: https://github.com/microsoft/devhome/issues/3714
    /// </summary>
    private ulong GetRequiredExtractedFileSize(VMGalleryImage image)
    {
        if (image.Publisher.Equals("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) ||
            image.Publisher.Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return GetExtractedFileSizeForWindowsImage(new Version(image.Version));
        }

        return DefaultImageExtractedSize;
    }

    private ulong GetExtractedFileSizeForWindowsImage(Version osVersion)
    {
        // Win11 image
        if (osVersion.Major == 10 && osVersion.Minor == 0 && osVersion.Build >= 22000)
        {
            return Windows11ImageExtractedSize;
        }

        // Only version of Windows other than Win11 in the gallery is Win10
        return Windows10ImageExtractedSize;
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _log.Debug("Disposing VMGalleryService");
            if (disposing)
            {
                _gallerySemaphore.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
