// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.PI.TelemetryEvents;

[EventData]
public class VisibilityStartEventData : TimedStartEventBase
{
    internal VisibilityStartEventData(string featureName, DateTime featureStartTime)
        : base(featureName, featureStartTime)
    {
    }
}
