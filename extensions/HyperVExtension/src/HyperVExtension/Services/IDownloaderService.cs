// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;

namespace HyperVExtension.Services;

/// <summary>
/// Interface for a service that can download files from the web.
/// </summary>
public interface IDownloaderService
{
    /// <summary>
    /// Starts file a download operation asynchronously from the web.
    /// </summary>
    /// <param name="subscriber">The subscriber who progress should be reported back to</param>
    /// <param name="sourceWebUri">The web uri that points to the location of the file</param>
    /// <param name="destinationFilePath">The file path that the downloaded file should be downloaded into</param>
    /// <param name="cancellationToken">A token that can allow the operation to be cancelled while it is running</param>
    /// <returns>A Task to start the download operation <returns>
    public Task StartDownloadAsync(IDownloadSubscriber subscriber, Uri sourceWebUri, string destinationFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a string from the web asynchronously.
    /// </summary>
    /// <param name="sourceWebUri">The web uri that points to the location of the file</param>
    /// <param name="cancellationToken">A token that can allow the operation to be cancelled while it is running</param>
    /// <returns>String content returned by web server</returns>
    public Task<string> DownloadStringAsync(string sourceWebUri, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a byte array from the web asynchronously.
    /// </summary>
    /// <param name="sourceWebUri">The web uri that points to the location of the file</param>
    /// <param name="cancellationToken">A token that can allow the operation to be cancelled while it is running</param>
    /// <returns>Content returned by web server represented as an array of bytes</returns>
    public Task<byte[]> DownloadByteArrayAsync(string sourceWebUri, CancellationToken cancellationToken);

    public Task<long> GetHeaderContentLength(Uri sourceWebUri, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a file based on its destinations absolute path is being downloaded by the downloaderService.
    /// </summary>
    /// <param name="destinationFilePath">The Absolute path to the file once it's downloaded.</param>
    /// <returns>True if the DownloaderService is downloading the file. False otherwise</returns>
    public bool IsFileBeingDownloaded(string destinationFilePath);
}
