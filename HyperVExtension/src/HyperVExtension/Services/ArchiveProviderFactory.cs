// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models.VirtualMachineCreation;

namespace HyperVExtension.Services;

/// <summary>
/// Provides a factory for creating archive providers based on the archive file extension.
/// </summary>
public sealed class ArchiveProviderFactory : IArchiveProviderFactory
{
    public IArchiveProvider CreateArchiveProvider(string extension)
    {
        if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new DotNetZipArchiveProvider();
        }

        throw new ArgumentException($"Unsupported archive extension {extension}");
    }
}
