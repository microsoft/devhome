// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using HyperVExtension.Models.VirtualMachineCreation;

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Used to generate the source code for the classes when we deserialize the Json recieved from the VM gallery
/// and any associated json.
/// .Net 8 requires a JsonSerializerContext to be used with the JsonSerializerOptions when
/// deserializing JSON into objects.
/// See : https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0
/// for more information
/// </summary>
[JsonSerializable(typeof(VMGalleryItemWithHashBase))]
[JsonSerializable(typeof(VMGalleryConfig))]
[JsonSerializable(typeof(VMGalleryDetail))]
[JsonSerializable(typeof(VMGalleryDisk))]
[JsonSerializable(typeof(VMGalleryImage))]
[JsonSerializable(typeof(VMGalleryImageList))]
[JsonSerializable(typeof(VMGalleryLogo))]
[JsonSerializable(typeof(VMGalleryRequirements))]
[JsonSerializable(typeof(VMGallerySymbol))]
[JsonSerializable(typeof(VMGalleryThumbnail))]
[JsonSerializable(typeof(VMGalleryCreationUserInput))]
public sealed partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
