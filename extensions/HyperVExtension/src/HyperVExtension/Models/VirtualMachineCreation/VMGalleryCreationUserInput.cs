// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Represents the user input for a VM gallery creation operation.
/// </summary>
public sealed class VMGalleryCreationUserInput
{
    public string NewEnvironmentName { get; set; } = string.Empty;

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SelectedImageListIndex { get; set; }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(VMGalleryCreationUserInput))]
public sealed partial class VMSourceGenerationContext : JsonSerializerContext
{
}
