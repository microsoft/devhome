// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents the an image json object in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryImage
{
    public string Name { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }

    public string Version { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public List<string> Description { get; set; } = new List<string>();

    public VMGalleryConfig Config { get; set; } = new VMGalleryConfig();

    public VMGalleryRequirements Requirements { get; set; } = new VMGalleryRequirements();

    public VMGalleryDisk Disk { get; set; } = new VMGalleryDisk();

    public VMGalleryLogo Logo { get; set; } = new VMGalleryLogo();

    public VMGallerySymbol Symbol { get; set; } = new VMGallerySymbol();

    public VMGalleryThumbnail Thumbnail { get; set; } = new VMGalleryThumbnail();

    public List<VMGalleryDetail> Details { get; set; } = new List<VMGalleryDetail>();
}
