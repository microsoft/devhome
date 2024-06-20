// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models.VirtualMachineCreation;

namespace HyperVExtension.Services;

public interface IArchiveProviderFactory
{
    public IArchiveProvider CreateArchiveProvider(string extension);
}
