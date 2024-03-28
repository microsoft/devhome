// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Represents the user input for a VM gallery creation operation.
/// </summary>
public sealed class VMGalleryCreationUserInput
{
    public string NewVirtualMachineName { get; set; } = string.Empty;

    public int SelectedImageListIndex { get; set; }
}
