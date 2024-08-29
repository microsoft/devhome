// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;

namespace DevHome.DevInsights.TelemetryEvents;

[EventData]
public class FocusStartEventData : TimedStartEventBase
{
    internal FocusStartEventData(string featureName, DateTime featureStartTime)
        : base(featureName, featureStartTime)
    {
    }
}
