// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Represents an operation to quickly create a virtual machine using the VM Gallery.
/// </summary>
public interface IVMGalleryVMCreationOperation : ICreateComputeSystemOperation, IDownloadSubscriber
{
}
