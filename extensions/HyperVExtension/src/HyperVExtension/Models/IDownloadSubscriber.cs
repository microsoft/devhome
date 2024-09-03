// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models.VirtualMachineCreation;

namespace HyperVExtension.Models;

public interface IDownloadSubscriber : IProgress<IOperationReport>
{
}
