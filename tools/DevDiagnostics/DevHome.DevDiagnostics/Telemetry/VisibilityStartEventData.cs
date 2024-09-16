// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;

namespace DevHome.DevDiagnostics.TelemetryEvents;

[EventData]
public class VisibilityStartEventData : TimedStartEventBase
{
    internal VisibilityStartEventData(string featureName, DateTime featureStartTime)
        : base(featureName, featureStartTime)
    {
    }
}
