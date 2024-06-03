// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents a VM gallery item that contains a uri and a hash.
/// </summary>
public abstract class VMGalleryItemWithHashBase
{
    public string Uri { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;
}
