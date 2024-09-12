// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.Environments;

[EventData]
public class EnvironmentLaunchEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string ProviderId { get; }

    public string Status { get; }

    public string? DisplayMessage { get; }

    public string? DiagnosticText { get; }

    public int HResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentLaunchEvent"/> class.
    /// </summary>
    /// <param name="providerId">The Id of the compute system provider that owns the compute system that is being launched</param>
    /// <param name="status">The status of the launch operation</param>
    /// <param name="result">Associated telemetry result for the operation</param>
    public EnvironmentLaunchEvent(
        string providerId,
        EnvironmentsTelemetryStatus status,
        TelemetryResult result)
    {
        ProviderId = providerId;
        Status = status.ToString();
        HResult = result.HResult;
        DisplayMessage = result.DisplayMessage;
        DiagnosticText = result.DiagnosticText;
    }

    // Inherited but unused.
    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
