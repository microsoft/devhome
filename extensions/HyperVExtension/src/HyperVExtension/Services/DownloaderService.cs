// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Extensions;
using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;
using Serilog;
using Windows.Storage;

namespace HyperVExtension.Services;

/// <summary>
/// A service to download files from the web.
/// </summary>
public class DownloaderService : IDownloaderService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DownloaderService));

    // Use the same default buffer size as the DefaultCopyBufferSize variable in the .Nets System.IO.Stream class
    // See: https://github.com/dotnet/runtime/blob/f0117c96ace4d475af63bce80d8afa31a740b836/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L128C46-L128C52
    // For comments on why this size was chosen.
    private const int TransferBufferSize = 81920;

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly Dictionary<string, FileDownloadMonitor> _destinationFileDownloadMap = new();

    private readonly object _lock = new();

    public DownloaderService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc cref="IDownloaderService.StartDownloadAsync"/>
    public async Task StartDownloadAsync(
        IProgress<IOperationReport> progressProvider,
        Uri sourceWebUri,
        string destinationFilePath,
        CancellationToken cancellationToken)
    {
        var downloadMonitor = new FileDownloadMonitor(progressProvider);

        try
        {
            lock (_lock)
            {
                // If the destination file is being downloaded already subscribe the progress provider
                // to the monitor and return. No extra work is needed by us.
                if (_destinationFileDownloadMap.TryGetValue(destinationFilePath, out var monitor))
                {
                    monitor.AddSubscriber(progressProvider);
                    return;
                }

                // If the destination file isn't being downloaded and it exists already throw an exception
                // since we're not overwriting it with a new download.
                if (File.Exists(destinationFilePath))
                {
                    throw new InvalidOperationException(
                        "Destination file already exists and is not currently being downloaded");
                }

                _destinationFileDownloadMap.Add(destinationFilePath, downloadMonitor);
            }

            var httpClient = _httpClientFactory.CreateClient();
            var totalBytesToReceive = GetTotalBytesToReceive(
                await httpClient.GetAsync(sourceWebUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken));

            var webFileStream = await httpClient.GetStreamAsync(sourceWebUri, cancellationToken);
            using var outputFileStream = File.OpenWrite(destinationFilePath);
            outputFileStream.SetLength(totalBytesToReceive);

            await downloadMonitor.StartAsync(
                    webFileStream,
                    outputFileStream,
                    TransferBufferSize,
                    totalBytesToReceive,
                    cancellationToken);

            downloadMonitor.StopMonitor();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Unable to complete download of file {destinationFilePath}");

            // Handle case where we added new subscribers to the monitor, but we threw an exception
            // before we started the download or during the download.
            downloadMonitor.StopMonitor(ex.Message);
            throw;
        }
        finally
        {
            lock (_lock)
            {
                // Remove destination file from the map now that we've completed the download
                _destinationFileDownloadMap.Remove(destinationFilePath);
            }
        }
    }

    /// <inheritdoc cref="IDownloaderService.DownloadStringAsync"/>
    public async Task<string> DownloadStringAsync(string sourceWebUri, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return await httpClient.GetStringAsync(sourceWebUri, cancellationToken);
    }

    /// <inheritdoc cref="IDownloaderService.DownloadByteArrayAsync"/>
    public async Task<byte[]> DownloadByteArrayAsync(string sourceWebUri, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return await httpClient.GetByteArrayAsync(sourceWebUri, cancellationToken);
    }

    public async Task<long> GetHeaderContentLength(Uri sourceWebUri, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return GetTotalBytesToReceive(await httpClient.GetAsync(sourceWebUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken));
    }

    private long GetTotalBytesToReceive(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentLength.HasValue)
        {
            return response.Content.Headers.ContentLength.Value;
        }

        // We should be able to get the content length from the response headers from the Microsoft servers.
        throw new InvalidOperationException("The content length of the response is not known.");
    }

    public bool IsFileBeingDownloaded(string destinationFilePath)
    {
        lock (_lock)
        {
            if (_destinationFileDownloadMap.TryGetValue(destinationFilePath, out var subscribers))
            {
                return true;
            }

            return false;
        }
    }
}
