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
internal sealed class FileDownloadMonitor
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(FileDownloadMonitor));

    private readonly object _lock = new();

    private readonly List<IProgress<IOperationReport>> _subscriberList = new();

    private readonly Progress<ByteTransferProgress> _progressReporter;

    private bool _downloadInProgress;

    private DownloadOperationReport _lastSentReport = new(new ByteTransferProgress());

    public FileDownloadMonitor(IProgress<IOperationReport> progressSubscriber)
    {
        AddSubscriber(progressSubscriber);
        _progressReporter = new(PublishProgress);
    }

    public void AddSubscriber(IProgress<IOperationReport> progressSubscriber)
    {
        lock (_lock)
        {
            if (_lastSentReport != null)
            {
                // Subscriber addition requested so we'll submit the last recorded
                // progress we received before adding it to the subscriber list
                progressSubscriber.Report(_lastSentReport);
            }

            _subscriberList.Add(progressSubscriber);
        }
    }

    private void PublishProgress(ByteTransferProgress transferProgress)
    {
        lock (_lock)
        {
            _lastSentReport = new DownloadOperationReport(transferProgress);
            _subscriberList.ForEach(subscriber => subscriber.Report(_lastSentReport));
        }
    }

    public async Task StartAsync(
        Stream source,
        Stream destination,
        int bufferSize,
        long totalBytesToExtract,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_downloadInProgress)
            {
                // Download already started, no need to attempt
                // to start it again.
                return;
            }

            _downloadInProgress = true;
        }

        await source.CopyToAsync(
            destination,
            _progressReporter,
            bufferSize,
            totalBytesToExtract,
            cancellationToken);

        StopMonitor();
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
            // Download already stopped.
            if (!_downloadInProgress)
            {
                return;
            }

            _downloadInProgress = false;
        }
    }
}
