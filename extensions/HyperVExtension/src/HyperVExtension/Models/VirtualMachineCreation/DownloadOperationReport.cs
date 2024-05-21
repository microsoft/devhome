// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VirtualMachineCreation;

public class DownloadOperationReport : IOperationReport
{
    public ReportKind ReportKind => ReportKind.Download;

    public string LocalizationKey => "DownloadInProgress";

    public ByteTransferProgress ProgressObject { get; private set; }

    public DownloadOperationReport(ByteTransferProgress progressObj)
    {
        ProgressObject = progressObj;
    }
}
