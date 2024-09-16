// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents.Environments;

[EventData]
public class EnvironmentOperationEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string ProviderId { get; }

    public string Status { get; }

    public string OperationName { get; }

    public string? AdditionalContext { get; }

    public string? DisplayMessage { get; }

    public string? DiagnosticText { get; }

    public int HResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentOperationEvent"/> class.
    /// </summary>
    /// <param name="status">The status of the compute system operation</param>
    /// <param name="computeSystemOperation">An enum representing the compute system operation that was invoked</param>
    /// <param name="providerId">The Id of the compute system provider that owns the compute system that is being launched</param>
    /// <param name="result">Associated telemtry result for the operation</param>
    public EnvironmentOperationEvent(
        EnvironmentsTelemetryStatus status,
        ComputeSystemOperations computeSystemOperation,
        string providerId,
        TelemetryResult result,
        string? additionalContext = null)
    {
        Status = status.ToString();
        OperationName = computeSystemOperation.ToString();
        ProviderId = providerId;
        HResult = result.HResult;
        DisplayMessage = result.DisplayMessage;
        DiagnosticText = result.DiagnosticText;
        AdditionalContext = additionalContext;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
