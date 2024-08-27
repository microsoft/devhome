// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.Services;
using Windows.Storage;

namespace HyperVExtension.UnitTest.Mocks;

public class DownloaderServiceMock : IDownloaderService
{
    private readonly int _totalIterations = 4;

    private readonly long _totalBytesToReceive = 1000;

    private readonly long _bytesReceivedEachIteration = 250;

    private readonly IHttpClientFactory _httpClientFactory;

    public DownloaderServiceMock(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task StartDownloadAsync(IDownloadSubscriber subscriber, Uri sourceWebUri, string destinationFilePath, CancellationToken cancellationToken)
    {
        var bytesReceivedSoFar = 0L;
        for (var i = 0; i < _totalIterations; i++)
        {
            await Task.Delay(100, cancellationToken);
            bytesReceivedSoFar += _bytesReceivedEachIteration;
            subscriber.Report(new DownloadOperationReport(new ByteTransferProgress(bytesReceivedSoFar, _totalBytesToReceive)));
        }

        var zipFile = await GetTestZipFileInPackage();
        var destinationFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(destinationFilePath));
        var newDownloadedFile = await destinationFolder.CreateFileAsync(zipFile.Name, CreationCollisionOption.ReplaceExisting);
        await zipFile.CopyAndReplaceAsync(newDownloadedFile);
    }

    public async Task<StorageFile> GetTestZipFileInPackage()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        return await StorageFile.GetFileFromPathAsync($@"{currentDirectory}\HyperVExtension.UnitTest\Assets\6CFDC8E5163679E32B9886CEEACEB95F8919B20799CA8E5A6207B9F72EFEFD40.zip");
    }

    public async Task<string> DownloadStringAsync(string sourceWebUri, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return await httpClient.GetStringAsync(sourceWebUri, cancellationToken);
    }

    public async Task<byte[]> DownloadByteArrayAsync(string sourceWebUri, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return await httpClient.GetByteArrayAsync(sourceWebUri, cancellationToken);
    }

    public async Task<long> GetHeaderContentLength(Uri sourceWebUri, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return 100L;
    }

    public bool IsFileBeingDownloaded(string destinationFilePath)
    {
        return true;
    }
}
