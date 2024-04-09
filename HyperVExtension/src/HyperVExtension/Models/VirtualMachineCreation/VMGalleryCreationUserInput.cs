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
