// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents a list of image json objects in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryImageList
{
    public List<VMGalleryImage> Images { get; set; } = new List<VMGalleryImage>();
}

// Uses .NET's JSON source generator support for serializing / deserializing VMGalleryImageList to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(VMGalleryImageList))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{
}
