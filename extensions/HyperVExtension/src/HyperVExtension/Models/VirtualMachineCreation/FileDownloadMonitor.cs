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

    private DownloadOperationReport? _lastSentReport;

    public FileDownloadMonitor(IProgress<IOperationReport> progressProvider)
    {
        AddSubscriber(progressProvider);
        _progressReporter = new(PublishProgress);
    }

    public void AddSubscriber(IProgress<IOperationReport> progressProvider)
    {
        lock (_lock)
        {
            if (_lastSentReport != null)
            {
                // Subscriber addition requested so we'll submit the last recorded
                // progress we received before adding it to the subscriber list
                progressProvider.Report(_lastSentReport);
            }

            _subscriberList.Add(progressProvider);
        }
    }

    private void PublishProgress(ByteTransferProgress transferProgress)
    {
        _lastSentReport = new DownloadOperationReport(transferProgress);
        _subscriberList.ForEach(subscriber => subscriber.Report(_lastSentReport));
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

            // Before starting the initial download, we'll update _lastSentReport so any
            // subscribers who are added before and after we leave the lock are notified.
            PublishProgress(new(bytesReceived: 0, totalBytesToReceive: totalBytesToExtract));
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
        lock (_lock)
        {
            // Download already stopped.
            if (!_downloadInProgress)
            {
                return;
            }

            _downloadInProgress = false;

            // Send error message to all subscribers.
            if (!string.IsNullOrEmpty(errorMessage))
            {
                PublishProgress(new ByteTransferProgress(errorMessage));
            }
        }
    }
}
