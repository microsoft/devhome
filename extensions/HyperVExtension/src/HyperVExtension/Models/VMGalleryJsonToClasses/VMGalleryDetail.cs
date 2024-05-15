// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents the 'detail' json object of an image in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryDetail
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
