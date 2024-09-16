// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents the 'disk' json object of an image in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryDisk : VMGalleryItemWithHashBase
{
    public string ArchiveRelativePath { get; set; } = string.Empty;

    public ulong ArchiveSizeInBytes { get; set; } = ulong.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating the total required disk space for the image
    /// after it is extracted from the archive file.
    /// </summary>
    public ulong ExtractedFileRequiredFreeSpace { get; set; } = ulong.MaxValue;
}
