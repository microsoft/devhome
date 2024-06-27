// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;

namespace HyperVExtension.Extensions;

public static class StreamExtensions
{
    /// <summary>
    /// Copies the contents of a source stream to a destination stream and reports the progress of the operation.
    /// </summary>
    /// <param name="source"> The source stream that data will be copied from </param>
    /// <param name="destination">The destination stream the data will be copied into </param>
    /// <param name="progressProvider">The object that progress will be reported to</param>
    /// <param name="bufferSize">The size of the buffer which is used to read data from the source stream and write it to the destination stream</param>
    /// <param name="cancellationToken">A cancellation token that will allow the caller to cancel the operation</param>
    public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<ByteTransferProgress> progressProvider, int bufferSize, long totalBytesToExtract, CancellationToken cancellationToken)
    {
        var buffer = new byte[bufferSize];
        long totalRead = 0;
        var lastPercentage = 0U;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            var progressPercentage = (uint)(totalRead / (double)totalBytesToExtract * 100D);

            // Only update progress when a whole percentage has been completed.
            if (progressPercentage != lastPercentage)
            {
                // Report the progress of the operation.
                progressProvider.Report(new ByteTransferProgress(totalRead, totalBytesToExtract));
                lastPercentage = progressPercentage;
            }
        }
    }
}
