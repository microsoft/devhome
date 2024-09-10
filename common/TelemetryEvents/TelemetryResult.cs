// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Windows.Win32.Foundation;

namespace DevHome.Common.TelemetryEvents;

public class TelemetryResult
{
    private const int SuccessHResult = 0;

    public int HResult { get; private set; }

    public ProviderOperationStatus Status { get; private set; }

    public string? DisplayMessage { get; private set; }

    public string? DiagnosticText { get; private set; }

    public TelemetryResult(ProviderOperationResult? result)
    {
        UpdateProperties(result);
    }

    public TelemetryResult(int hResult, string displayMessage, string diagnosticText)
    {
        HResult = hResult;
        Status = ProviderOperationStatus.Failure;
        DisplayMessage = displayMessage;
        DiagnosticText = diagnosticText;
    }

    public TelemetryResult()
    {
        HResult = SuccessHResult;
        Status = ProviderOperationStatus.Success;
    }

    private void UpdateProperties(ProviderOperationResult? result)
    {
        if (result == null)
        {
            // The extension provided us with a null ProviderOperationResult,
            // so the telemetry should state this explicitly.
            Status = ProviderOperationStatus.Failure;
            HResult = HRESULT.E_INVALIDARG;
            DiagnosticText = "ProviderOperationResult was null";
            DisplayMessage = "ProviderOperationResult was null";
            return;
        }

        Status = result.Status;
        DiagnosticText = result.DiagnosticText;
        DisplayMessage = result.DisplayMessage;
        HResult = SuccessHResult;

        if (Status == ProviderOperationStatus.Failure)
        {
            HResult = result.ExtendedError.HResult;
        }
    }
}
