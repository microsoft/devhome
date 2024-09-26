// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepositoryManagement;

[EventData]
public class RepositoryLineItemErrorEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public string Action { get; } = string.Empty;

    public int Hresult { get; }

    public string ErrorMessage { get; } = string.Empty;

    public RepositoryLineItemErrorEvent(string action, int hresult, string errorMessage)
    {
        Action = action;
        Hresult = hresult;
        ErrorMessage = errorMessage;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace
    }
}
