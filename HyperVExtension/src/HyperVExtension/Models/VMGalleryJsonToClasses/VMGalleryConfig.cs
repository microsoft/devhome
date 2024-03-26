// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents the 'config' json object of an image in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryConfig
{
    public string SecureBoot { get; set; } = string.Empty;

    public string EnhancedSessionTransportType { get; set; } = string.Empty;
}
