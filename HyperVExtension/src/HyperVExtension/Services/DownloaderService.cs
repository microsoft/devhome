// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Extensions;
using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;

namespace HyperVExtension.Services;

/// <summary>
/// A service to download files from the web.
/// </summary>
public class DownloaderService : IDownloaderService
{
    // Use the same default buffer size as the DefaultCopyBufferSize variable in the .Nets System.IO.Stream class
    // See: https://github.com/dotnet/runtime/blob/f0117c96ace4d475af63bce80d8afa31a740b836/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L128C46-L128C52
    // For comments on why this size was chosen.
    private const int _transferBufferSize = 81920;

    private readonly IHttpClientFactory _httpClientFactory;

    public DownloaderService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc cref="IDownloaderService.StartDownloadAsync"/>
    public async Task StartDownloadAsync(IProgress<IOperationReport> progressProvider, Uri sourceWebUri, string destinationFile, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var totalBytesToReceive = GetTotalBytesToReceive(await httpClient.GetAsync(sourceWebUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken));
        var webFileStream = await httpClient.GetStreamAsync(sourceWebUri, cancellationToken);
        using var outputFileStream = File.OpenWrite(destinationFile);
        outputFileStream.SetLength(totalBytesToReceive);

        var downloadProgress = new Progress<ByteTransferProgress>(progressObj =>
        {
            progressProvider.Report(new DownloadOperationReport(progressObj));
        });

        await webFileStream.CopyToAsync(outputFileStream, downloadProgress, _transferBufferSize, totalBytesToReceive, cancellationToken);
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
}
