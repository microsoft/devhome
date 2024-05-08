// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Storage;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Represents an interface that all archive providers can inherit from.
/// </summary>
public interface IArchiveProvider
{
    /// <summary>
    /// Extracts the contents of the archive file into the destination folder.
    /// </summary>
    /// <param name="progressProvider">The provider who progress should be reported back to</param>
    /// <param name="archivedFile">An archive file on the file system that can be extracted</param>
    /// <param name="destinationAbsoluteFilePath">The absolute file path in the file system that the archive file will be extracted to</param>
    /// <param name="cancellationToken">A token that can allow the operation to be cancelled while it is running</param>
    public Task ExtractArchiveAsync(IProgress<IOperationReport> progressProvider, StorageFile archivedFile, string destinationAbsoluteFilePath, CancellationToken cancellationToken);
}
