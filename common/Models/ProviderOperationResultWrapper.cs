// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Models;

/// <summary>
/// Wrapper class for the ProviderOperationResult class. This class is used to
/// when passing ProviderOperationResult objects between processes.
/// </summary>
public class ProviderOperationResultWrapper
{
    public ProviderOperationStatus Status { get; set; }

    public Exception ExtendedError { get; set; }

    public string DisplayMessage { get; set; }

    public string DiagnosticText { get; set; }

    public ProviderOperationResultWrapper(ProviderOperationResult result)
    {
        Status = result.Status;
        ExtendedError = result.ExtendedError;
        DisplayMessage = result.DisplayMessage;
        DiagnosticText = result.DiagnosticText;
    }
}
