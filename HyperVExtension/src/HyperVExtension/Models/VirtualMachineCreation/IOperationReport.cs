// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models.VirtualMachineCreation;

public enum ReportKind
{
    ArchiveExtraction,
    Download,
}

public interface IOperationReport
{
    public ReportKind ReportKind { get; }

    public string LocalizationKey { get; }

    public ulong BytesReceived { get; }

    public ulong TotalBytesToReceive { get; }
}
