// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VirtualMachineCreation;

public class DownloadOperationReport : IOperationReport
{
    public ReportKind ReportKind => ReportKind.Download;

    public string LocalizationKey => "DownloadInProgress";

    public ulong BytesReceived { get; private set; }

    public ulong TotalBytesToReceive { get; private set; }

    public DownloadOperationReport(ulong bytesReceived, ulong totalBytesToReceive)
    {
        BytesReceived = bytesReceived;
        TotalBytesToReceive = totalBytesToReceive;
    }
}
