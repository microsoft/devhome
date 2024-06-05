// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.QuickstartPlayground;

[EventData]
public class ProjectGenerationErrorInfo(string errorMessage, Exception extendedError, string diagnosticText) : EventBase
{
    public string ErrorMessage { get; } = errorMessage;

    public string ExtendedError { get; } = extendedError.ToString();

    public string DiagnosticText { get; } = diagnosticText;

    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
