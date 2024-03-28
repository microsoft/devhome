// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents the 'disk' json object of an image in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryDisk : VMGalleryItemWithHashBase
{
    public string ArchiveRelativePath { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }
}
