// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using HyperVExtension.Extensions;
using Windows.Storage;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Provides methods to extract a zip archive. This class uses the .NET <see cref="ZipArchive"/> class to extract the contents of the archive file.
/// <see cref="ZipArchive"/> is used by Hyper-V Manager's VM Quick Create feature. This gives us parity, but in the future it is expected that faster/more efficient
/// archive extraction libraries will be added.
/// </summary>
/// <remarks>
/// .Net core's ZipFile and ZipArchive implementations for extracting large files (GBs) are slow when used in Dev Homes Debug configuration.
/// In release they are much quicker. To experience downloads from the users point of view build with the release configuration.
/// </remarks>
public sealed class DotNetZipArchiveProvider : IArchiveProvider
{
    // Same buffer size used by Hyper-V Manager's VM gallery feature.
    private readonly int _transferBufferSize = 4096;

    /// <inheritdoc cref="IArchiveProvider.ExtractArchiveAsync"/>
    public async Task ExtractArchiveAsync(IProgress<IOperationReport> progressProvider, StorageFile archivedFile, string destinationAbsoluteFilePath, CancellationToken cancellationToken)
    {
        using var zipArchive = ZipFile.OpenRead(archivedFile.Path);

        // Expect only one entry in the zip file, which would be the virtual disk.
        var zipArchiveEntry = zipArchive.Entries.First();
        var totalBytesToExtract = zipArchiveEntry.Length;
        using var outputFileStream = File.OpenWrite(destinationAbsoluteFilePath);
        using var zipArchiveEntryStream = zipArchiveEntry.Open();

        var fileExtractionProgress = new Progress<ByteTransferProgress>(progressObj =>
        {
            progressProvider.Report(new ArchiveExtractionReport(progressObj));
        });

        outputFileStream.SetLength(totalBytesToExtract);
        await zipArchiveEntryStream.CopyToAsync(outputFileStream, fileExtractionProgress, _transferBufferSize, totalBytesToExtract, cancellationToken);
        File.SetLastWriteTime(destinationAbsoluteFilePath, zipArchiveEntry.LastWriteTime.DateTime);
    }
}
