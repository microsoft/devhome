// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models;

public enum TransferStatus
{
    NotStarted,
    InProgress,
    Succeeded,
    Failed,
}

/// <summary>
/// Represents progress of an operation that require transferring bytes from one place to another.
/// </summary>
public class ByteTransferProgress
{
    private readonly TransferStatus _transferStatus;

    public long BytesReceived { get; }

    public long TotalBytesToReceive { get; }

    public uint PercentageComplete => (uint)((BytesReceived / (double)TotalBytesToReceive) * 100);

    public string ErrorMessage { get; } = string.Empty;

    public ByteTransferProgress()
    {
        _transferStatus = TransferStatus.NotStarted;
    }

    public ByteTransferProgress(
        long bytesReceived,
        long totalBytesToReceive,
        TransferStatus transferStatus = TransferStatus.InProgress)
    {
        BytesReceived = bytesReceived;
        TotalBytesToReceive = totalBytesToReceive;
        _transferStatus = transferStatus;
    }

    public ByteTransferProgress(string errorMessage)
    {
        ErrorMessage = errorMessage;
        _transferStatus = TransferStatus.Failed;
    }

    public bool Succeeded => _transferStatus == TransferStatus.Succeeded;

    public bool Failed => _transferStatus == TransferStatus.Failed;

    public bool Ended => Succeeded || Failed;
}
