// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents the 'requirements' json object of an image in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryRequirements
{
    public string DiskSpace { get; set; } = string.Empty;
}
