// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progressProvider, int bufferSize, CancellationToken cancellationToken)
    {
        var buffer = new byte[bufferSize];
        long totalRead = 0;

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

            // Report the progress of the operation.
            progressProvider.Report(totalRead);
        }
    }
}
