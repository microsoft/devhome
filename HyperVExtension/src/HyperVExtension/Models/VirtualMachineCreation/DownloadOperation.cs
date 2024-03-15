// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VirtualMachineCreation;

public class DownloadOperation : IOperationReport
{
    public ReportKind ReportKind => ReportKind.Download;

    public string LocalizationKey => "DownloadInProgress";

    public ulong BytesReceived { get; private set; }

    public ulong TotalBytesToReceive { get; private set; }

    public DownloadOperation(ulong bytesReceived, ulong totalBytesToReceive)
    {
        BytesReceived = bytesReceived;
        TotalBytesToReceive = totalBytesToReceive;
    }
}
