// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace HyperVExtension.Models.VirtualMachineCreation;

/// <summary>
/// Represents an operation to extract an archive file.
/// </summary>
public sealed class ArchiveExtractionReport : IOperationReport
{
    public ReportKind ReportKind => ReportKind.ArchiveExtraction;

    public string LocalizationKey => "ExtractionInProgress";

    public ByteTransferProgress ProgressObject { get; private set; }

    public ArchiveExtractionReport(ByteTransferProgress progressObj)
    {
        ProgressObject = progressObj;
    }
}
