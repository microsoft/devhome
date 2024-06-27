// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models;

/// <summary>
/// Represents progress of an operation that require transferring bytes from one place to another.
/// </summary>
public class ByteTransferProgress
{
    public long BytesReceived { get; set; }

    public long TotalBytesToReceive { get; set; }

    public uint PercentageComplete => (uint)((BytesReceived / (double)TotalBytesToReceive) * 100);

    public ByteTransferProgress(long bytesReceived, long totalBytesToReceive)
    {
        BytesReceived = bytesReceived;
        TotalBytesToReceive = totalBytesToReceive;
    }
}
