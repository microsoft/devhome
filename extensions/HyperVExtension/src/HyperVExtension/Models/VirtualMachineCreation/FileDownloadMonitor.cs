// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Extensions;
using Serilog;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Monitors the download of a file and allows clients to subscribe to the progress of the
/// download. As new progress comes in the FileDownloadMonitor publishes this progress to
/// its subscribers.
/// </summary>
public sealed class FileDownloadMonitor : IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(FileDownloadMonitor));

    private readonly object _lock = new();

    private readonly List<IDownloadSubscriber> _subscriberList = new();

    private readonly Progress<ByteTransferProgress> _progressReporter;

    private readonly SemaphoreSlim _downloadCompletionSemaphore = new(0);

    private CancellationTokenSource _cancellationTokenSource = new();

    private bool _downloadInProgress;

    private bool _disposed;

    private DownloadOperationReport _lastSentReport = new(new ByteTransferProgress());

    public FileDownloadMonitor(IDownloadSubscriber subscriber)
    {
        AddSubscriber(subscriber);
        _progressReporter = new(PublishProgress);
    }

    public void AddSubscriber(IDownloadSubscriber subscriber)
    {
        lock (_lock)
        {
            if (_lastSentReport != null)
            {
                // Subscriber addition requested so we'll submit the last recorded
                // progress we received before adding it to the subscriber list
                subscriber.Report(_lastSentReport);
            }

            _subscriberList.Add(subscriber);
        }
    }

    private void PublishProgress(ByteTransferProgress progress)
    {
        var subscriberCount = 0;

        lock (_lock)
        {
            _lastSentReport = new DownloadOperationReport(progress);
            _subscriberList.ForEach(subscriber => subscriber.Report(_lastSentReport));
            subscriberCount = _subscriberList.Count;
        }

        if (progress.Ended && subscriberCount > 0)
        {
            // Release the waiting threads now that the download has ended.
            _downloadCompletionSemaphore.Release(subscriberCount);
        }
    }

    private int GetSubscriberCount()
    {
        lock (_lock)
        {
            return _subscriberList.Count;
        }
    }

    /// <summary>
    /// Allows subscribers who subscribe to the download after it has started to wait
    /// for it to complete.
    /// </summary>
    public async Task WaitForDownloadCompletionAsync(CancellationToken cancellationToken)
    {
        while (!IsDownloadComplete())
        {
            await _downloadCompletionSemaphore.WaitAsync(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    public bool IsDownloadComplete()
    {
        lock (_lock)
        {
            return _lastSentReport.ProgressObject.Ended;
        }
    }

    public void CancelDownload()
    {
        _cancellationTokenSource.Cancel();
    }

    public void Start(
        HttpClient client,
        Uri sourceWebUri,
        string destinationFilePath,
        int bufferSize,
        long totalBytesToReceive)
    {
        lock (_lock)
        {
            if (_downloadInProgress || _lastSentReport.ProgressObject.Ended)
            {
                // Download already in started or already ended.
                return;
            }

            _downloadInProgress = true;
            _cancellationTokenSource = new();
        }

        // Start download
        _ = Task.Run(
            async () =>
            {
                try
                {
                    using var webFileStream =
                        await client.GetStreamAsync(sourceWebUri, _cancellationTokenSource.Token);
                    using var outputFileStream = File.OpenWrite(destinationFilePath);
                    outputFileStream.SetLength(totalBytesToReceive);

                    await webFileStream.CopyToAsync(
                        outputFileStream,
                        _progressReporter,
                        bufferSize,
                        totalBytesToReceive,
                        _cancellationTokenSource.Token);

                    StopMonitor();
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Download of file '{destinationFilePath}' failed");
                    StopMonitor(ex.Message);

                    if (File.Exists(destinationFilePath))
                    {
                        // Delete unfinished download file.
                        File.Delete(destinationFilePath);
                    }
                }
            });
    }

    public void StopMonitor(string? errorMessage = null)
    {
        // Send error message to all subscribers.
        if (!string.IsNullOrEmpty(errorMessage))
        {
            PublishProgress(new ByteTransferProgress(errorMessage));
        }
        else
        {
            var lastProgress = _lastSentReport.ProgressObject;
            PublishProgress(
                new ByteTransferProgress(
                    lastProgress.BytesReceived,
                    lastProgress.TotalBytesToReceive,
                    TransferStatus.Succeeded));
        }

        lock (_lock)
        {
            _downloadInProgress = false;
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _log.Debug("Disposing FileDownloadMonitor");
            if (disposing)
            {
                _cancellationTokenSource.Dispose();
                _downloadCompletionSemaphore.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
